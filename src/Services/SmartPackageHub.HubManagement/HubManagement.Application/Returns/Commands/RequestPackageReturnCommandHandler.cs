using HubManagement.Application.Returns.DTOs;
using HubManagement.Application.Returns.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Encoding;
using SmartPackageHub.Common.Services.Messaging.Guards;
using SmartPackageHub.Common.Services.Messaging.Models;

namespace HubManagement.Application.Returns.Commands
{
    public sealed class RequestPackageReturnCommandHandler
        : CreateCommandHandler<RequestPackageReturnCommand, PackageReturnCommandResult>
    {
        private static readonly Action<ILogger, Guid, Guid, Exception?> LogReturnRequested =
            LoggerMessage.Define<Guid, Guid>(
                LogLevel.Information,
                new EventId(1, nameof(RequestPackageReturnCommandHandler)),
                "Return {ReturnId} requested for package {PackageId}");

        private static readonly Action<ILogger, Guid, Exception?> LogReturnNotifyFailed =
            LoggerMessage.Define<Guid>(
                LogLevel.Warning,
                new EventId(2, nameof(RequestPackageReturnCommandHandler)),
                "Return confirmation notification failed for return {ReturnId}");

        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageReturn> _returns;
        private readonly IRepository<Delivery> _deliveries;
        private readonly IRepository<SocietyCarrierReturnPolicy> _carrierPolicies;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Notification> _notifications;
        private readonly IRepository<AuditLog> _auditLogs;
        private readonly IOutboundMessageQueue _outboundQueue;
        private readonly ILogger<RequestPackageReturnCommandHandler> _logger;

        public RequestPackageReturnCommandHandler(
            IRepository<Package> packages,
            IRepository<PackageReturn> returns,
            IRepository<Delivery> deliveries,
            IRepository<SocietyCarrierReturnPolicy> carrierPolicies,
            IRepository<Resident> residents,
            IRepository<Notification> notifications,
            IRepository<AuditLog> auditLogs,
            IOutboundMessageQueue outboundQueue,
            ILogger<RequestPackageReturnCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _returns = returns;
            _deliveries = deliveries;
            _carrierPolicies = carrierPolicies;
            _residents = residents;
            _notifications = notifications;
            _auditLogs = auditLogs;
            _outboundQueue = outboundQueue;
            _logger = logger;
        }

        public override async Task<PackageReturnCommandResult> Handle(
            RequestPackageReturnCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var payload = request.Request ?? throw new ArgumentNullException(nameof(request));

            if (!PackageReturnReasonParser.TryParse(payload.Reason, out var reason))
                return PackageReturnCommandResult.Fail("Return reason is required and must be a supported value.");

            var gate = await ValidateReturnRequestAsync(request.PackageId, payload.ResidentId).ConfigureAwait(false);
            if (gate.Error is not null)
                return gate.Error;

            var package = gate.Package!;
            var packageResidentId = gate.ResidentId;

            PackageReturn packageReturn;
            try
            {
                packageReturn = PackageReturn.Create(
                    package.Id,
                    package.SocietyId,
                    packageResidentId,
                    reason,
                    payload.Note);
            }
            catch (ArgumentException ex)
            {
                return PackageReturnCommandResult.Fail(ex.Message);
            }

            var resident = await _residents.GetByIdAsync(packageResidentId).ConfigureAwait(false);
            package.BeginReturn();
            _returns.Insert(packageReturn);
            _auditLogs.Insert(AuditLog.Create(
                package.SocietyId,
                resident?.UserId ?? packageResidentId,
                nameof(PackageReturn),
                packageReturn.Id,
                "ReturnRequested",
                DateTime.UtcNow,
                $"{{\"reason\":\"{reason}\",\"packageId\":\"{package.Id}\"}}"));

            // Same commit as the return so Redis/SMS cannot delay the mobile response.
            var smsToEnqueue = PrepareReturnRequestedNotices(package, packageReturn, resident);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogReturnRequested(_logger, packageReturn.Id, package.Id, null);
            await TryEnqueueReturnSmsAsync(packageReturn.Id, smsToEnqueue, cancellationToken).ConfigureAwait(false);

            return PackageReturnCommandResult.Ok(
                PackageReturnMapper.ToDto(packageReturn, package: package, resident: resident),
                "Return request received. Staff will guide pickup/drop.");
        }

        private async Task<(PackageReturnCommandResult? Error, Package? Package, Guid ResidentId)> ValidateReturnRequestAsync(
            Guid packageId,
            Guid residentId)
        {
            var package = await _packages.GetByIdAsync(packageId).ConfigureAwait(false);
            if (package is null)
                return (PackageReturnCommandResult.NotFound("Package was not found."), null, Guid.Empty);

            if (package.Status != PackageStatus.Delivered)
                return (PackageReturnCommandResult.Fail(
                    "Request return is only available for delivered packages.",
                    conflict: true), null, Guid.Empty);

            if (package.ResidentId is not Guid packageResidentId || packageResidentId == Guid.Empty)
                return (PackageReturnCommandResult.Fail("Package has no resident assigned."), null, Guid.Empty);

            if (residentId == Guid.Empty || residentId != packageResidentId)
                return (PackageReturnCommandResult.Fail("Resident does not own this package."), null, Guid.Empty);

            var openExists = _returns.FetchMulti(r =>
                    r.PackageId == package.Id &&
                    (r.Status == PackageReturnStatus.ReturnRequested ||
                     r.Status == PackageReturnStatus.ReturnReceived ||
                     r.Status == PackageReturnStatus.ReturnShelved),
                noTracking: true)
                .Any();
            if (openExists)
                return (PackageReturnCommandResult.Fail(
                    "An open return already exists for this package.",
                    conflict: true), null, Guid.Empty);

            var deliveredAt = ResolveDeliveredAt(package.Id);
            if (deliveredAt is null)
                return (PackageReturnCommandResult.Fail(
                    "Delivery timestamp is missing; return cannot be requested yet.",
                    conflict: true), null, Guid.Empty);

            var windowDays = await ResolveReturnWindowDaysAsync(package).ConfigureAwait(false);
            if (DateTime.UtcNow > deliveredAt.Value.AddDays(windowDays))
            {
                return (PackageReturnCommandResult.Fail(
                    $"Return window of {windowDays} day(s) has expired.",
                    conflict: true), null, Guid.Empty);
            }

            return (null, package, packageResidentId);
        }

        private async Task TryEnqueueReturnSmsAsync(
            Guid returnId,
            OutboundMessageWorkItem? smsToEnqueue,
            CancellationToken cancellationToken)
        {
            if (smsToEnqueue is null)
                return;

            try
            {
                using var enqueueCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                enqueueCts.CancelAfter(TimeSpan.FromMilliseconds(750));
                await _outboundQueue.EnqueueAsync(smsToEnqueue, enqueueCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogReturnNotifyFailed(_logger, returnId, ex);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                LogReturnNotifyFailed(_logger, returnId, null);
            }
        }

        private DateTime? ResolveDeliveredAt(Guid packageId)
        {
            return _deliveries.FetchMulti(d => d.PackageId == packageId, noTracking: true)
                .AsEnumerable()
                .Where(d => d.Status.Equals(DeliveryStatus.Completed))
                .OrderByDescending(d => d.DeliveredAt)
                .Select(d => (DateTime?)d.DeliveredAt)
                .FirstOrDefault();
        }

        private Task<int> ResolveReturnWindowDaysAsync(Package package)
        {
            var carrier = (package.CourierPartner ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(carrier))
                return Task.FromResult(SocietyCarrierReturnPolicy.DefaultReturnWindowDays);

            var policy = _carrierPolicies.FetchMulti(p => p.SocietyId == package.SocietyId, noTracking: true)
                .AsEnumerable()
                .FirstOrDefault(p => string.Equals(p.CarrierName, carrier, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(policy?.ReturnWindowDays ?? SocietyCarrierReturnPolicy.DefaultReturnWindowDays);
        }

        /// <summary>
        /// Inserts in-app/push + optional SMS notification rows for the pending unit of work.
        /// Returns an SMS queue item when SMS should be enqueued after commit (never blocks commit).
        /// </summary>
        private OutboundMessageWorkItem? PrepareReturnRequestedNotices(
            Package package,
            PackageReturn packageReturn,
            Resident? resident)
        {
            if (resident is null)
                return null;

            const string templateName = "ReturnRequested";
            var body = SmsEncodingHelper.ToGsm7SafeOtpMessage(
                "Return request received. Staff will guide pickup/drop. " +
                $"Package: {package.PkgNumber}. " +
                $"Courier: {package.CourierPartner ?? "Courier"}.");

            var push = Notification.Create(
                package.SocietyId,
                resident.Id,
                package.Id,
                NotificationChannel.Create(ChannelType.Push, resident.Id.ToString("D")),
                MessageContent.Create(templateName, body));
            push.MarkAsSent();
            _notifications.Insert(push);

            var phone = resident.PhoneNumber;
            if (phone is not { Length: > 0 } ||
                !PhoneNumberGuard.TryNormalizeIndiaMobile(phone, out var local10))
            {
                return null;
            }

            var idempotencyKey = $"return:{packageReturn.Id:N}:{templateName}:{ChannelType.SMS}";
            var segments = SmsEncodingHelper.EstimateSegmentCount(body);
            var sms = Notification.Create(
                package.SocietyId,
                resident.Id,
                package.Id,
                NotificationChannel.Create(ChannelType.SMS, local10),
                MessageContent.Create(templateName, body),
                idempotencyKey,
                segmentCount: segments);
            _notifications.Insert(sms);

            return new OutboundMessageWorkItem(
                sms.Id,
                ChannelType.SMS.ToString(),
                idempotencyKey,
                DateTime.UtcNow);
        }
    }
}
