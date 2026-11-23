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

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            if (package is null)
                return PackageReturnCommandResult.NotFound("Package was not found.");

            if (package.Status != PackageStatus.Delivered)
                return PackageReturnCommandResult.Fail(
                    "Request return is only available for delivered packages.",
                    conflict: true);

            if (package.ResidentId is not Guid packageResidentId || packageResidentId == Guid.Empty)
                return PackageReturnCommandResult.Fail("Package has no resident assigned.");

            if (payload.ResidentId == Guid.Empty || payload.ResidentId != packageResidentId)
                return PackageReturnCommandResult.Fail("Resident does not own this package.");

            var openExists = _returns.FetchMulti(r =>
                    r.PackageId == package.Id &&
                    (r.Status == PackageReturnStatus.ReturnRequested ||
                     r.Status == PackageReturnStatus.ReturnReceived ||
                     r.Status == PackageReturnStatus.ReturnShelved),
                noTracking: true)
                .Any();
            if (openExists)
                return PackageReturnCommandResult.Fail(
                    "An open return already exists for this package.",
                    conflict: true);

            var deliveredAt = ResolveDeliveredAt(package.Id);
            if (deliveredAt is null)
                return PackageReturnCommandResult.Fail(
                    "Delivery timestamp is missing; return cannot be requested yet.",
                    conflict: true);

            var windowDays = await ResolveReturnWindowDaysAsync(package).ConfigureAwait(false);
            if (DateTime.UtcNow > deliveredAt.Value.AddDays(windowDays))
                return PackageReturnCommandResult.Fail(
                    $"Return window of {windowDays} day(s) has expired.",
                    conflict: true);

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

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogReturnRequested(_logger, packageReturn.Id, package.Id, null);

            try
            {
                await QueueReturnRequestedNoticeAsync(package, packageReturn, resident, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogReturnNotifyFailed(_logger, packageReturn.Id, ex);
            }

            return PackageReturnCommandResult.Ok(
                PackageReturnMapper.ToDto(packageReturn, package: package, resident: resident),
                "Return request received. Staff will guide pickup/drop.");
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

        private async Task QueueReturnRequestedNoticeAsync(
            Package package,
            PackageReturn packageReturn,
            Resident? resident,
            CancellationToken cancellationToken)
        {
            if (resident is null)
                return;

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
            if (phone is { Length: > 0 } && PhoneNumberGuard.TryNormalizeIndiaMobile(phone, out var local10))
            {
                var idempotencyKey = $"return:{packageReturn.Id:N}:{templateName}:{ChannelType.SMS}";
                var existingSms = await _notifications.FirstOrDefaultAsync(n => n.IdempotencyKey == idempotencyKey)
                    .ConfigureAwait(false);
                if (existingSms is null)
                {
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
                    await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
                    await _outboundQueue.EnqueueAsync(
                        new OutboundMessageWorkItem(
                            sms.Id,
                            ChannelType.SMS.ToString(),
                            idempotencyKey,
                            DateTime.UtcNow),
                        cancellationToken).ConfigureAwait(false);
                    return;
                }
            }

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
