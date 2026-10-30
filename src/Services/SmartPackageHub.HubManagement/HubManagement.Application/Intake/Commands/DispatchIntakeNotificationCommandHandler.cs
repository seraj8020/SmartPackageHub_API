using System.Globalization;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Encoding;
using SmartPackageHub.Common.Services.Messaging.Guards;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Models;

namespace HubManagement.Application.Intake.Commands
{
    /// <summary>
    /// Intake notice path: push immediately; SMS left Pending and enqueued (same Redis/worker path), GSM-safe body, idempotent key.
    /// </summary>
    public sealed class DispatchIntakeNotificationCommandHandler : UpdateCommandHandler<DispatchIntakeNotificationCommand, bool>
    {
        private static readonly Action<ILogger, string, string?, Exception?> LogDispatchIntakeResidentMissing =
            LoggerMessage.Define<string, string?>(
                LogLevel.Information,
                new EventId(1, nameof(DispatchIntakeNotificationCommandHandler)),
                "Intake {Awb} has no resident yet. Notice: {Body}");
        private static readonly Action<ILogger, string, string, Exception?> LogDispatchNotificationToResident =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(1, nameof(DispatchIntakeNotificationCommandHandler)),
                "Dispatched push + queued SMS intake notice for {Awb} to {Resident}");
        private readonly IRepository<PackageIntake> _intakes;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<OtpLog> _otpLogs;
        private readonly IRepository<Notification> _notifications;
        private readonly IOutboundMessageQueue _outboundQueue;
        private readonly ILogger<DispatchIntakeNotificationCommandHandler> _logger;

        public DispatchIntakeNotificationCommandHandler(
            IRepository<PackageIntake> intakes,
            IRepository<Package> packages,
            IRepository<Resident> residents,
            IRepository<Shelf> shelves,
            IRepository<OtpLog> otpLogs,
            IRepository<Notification> notifications,
            IOutboundMessageQueue outboundQueue,
            ILogger<DispatchIntakeNotificationCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _intakes = intakes;
            _packages = packages;
            _residents = residents;
            _shelves = shelves;
            _otpLogs = otpLogs;
            _notifications = notifications;
            _outboundQueue = outboundQueue;
            _logger = logger;
        }

        public override async Task<bool> Handle(DispatchIntakeNotificationCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var intake = await _intakes.GetByIdAsync(request.IntakeId).ConfigureAwait(false);
            if (intake == null)
                return false;

            var package = await _packages.GetByIdAsync(intake.PackageId).ConfigureAwait(false);
            if (package == null)
                return false;

            var awaitingCodApproval = package.IsCod && package.CodApprovalStatus == CodApprovalStatus.Pending;
            var templateName = awaitingCodApproval ? "CodApprovalRequired" : "IntakeReceived";
            var alreadySent = _notifications.GetAll().Any(notification =>
                notification.PackageId == package.Id && notification.Content.TemplateName == templateName);
            if (alreadySent)
                return true;

            var resident = package.ResidentId is Guid residentId
                ? await _residents.GetByIdAsync(residentId).ConfigureAwait(false)
                : null;
            var shelfCode = package.ShelfId is Guid shelfId
                ? (await _shelves.GetByIdAsync(shelfId).ConfigureAwait(false))?.Code
                : null;
            var otp = _otpLogs.GetAll()
                .Where(log => log.PackageId == package.Id && log.Purpose == "Pickup")
                .AsEnumerable()
                .OrderByDescending(log => log.Expiry?.Value)
                .Select(log => log.Token?.Value)
                .FirstOrDefault();

            var unit = string.IsNullOrWhiteSpace(package.TowerWing) ? package.FlatNumber.Value : $"{package.TowerWing}-{package.FlatNumber.Value}";
            var phone = resident?.PhoneNumber;
            var amount = package.CodAmount?.ToString("0.##", CultureInfo.InvariantCulture) ?? "0";

            // Keep intake SMS GSM-7 (no rupee symbol / fancy dashes) to avoid UCS-2 double billing.
            var body = awaitingCodApproval
                ? $"COD package awaiting your approval. " +
                  $"Courier: {package.CourierPartner ?? "Courier"}. " +
                  $"AWB: {package.CourierAwb}. " +
                  $"Amount: Rs {amount}. " +
                  $"Flat: {unit}. " +
                  "Approve or reject in the resident app to continue shelving."
                : $"Package received at Smart Package Hub. " +
                  $"Courier: {package.CourierPartner ?? "Courier"}. " +
                  $"AWB: {package.CourierAwb}. " +
                  $"Flat: {unit}. " +
                  $"Shelf: {shelfCode ?? "unassigned"}. " +
                  $"Pickup OTP: {otp ?? "pending"}.";

            body = SmsEncodingHelper.ToGsm7SafeOtpMessage(body);

            if (resident == null)
            {
                LogDispatchIntakeResidentMissing(_logger, package.CourierAwb, body, null);
                return true;
            }

            var push = Notification.Create(
                package.SocietyId,
                resident.Id,
                package.Id,
                NotificationChannel.Create(ChannelType.Push, resident.Id.ToString()),
                MessageContent.Create(templateName, body));
            push.MarkAsSent();
            _notifications.Insert(push);

            if (phone is { Length: > 0 } && PhoneNumberGuard.TryNormalizeIndiaMobile(phone, out var local10))
            {
                var idempotencyKey = $"intake:{package.Id:N}:{templateName}:{ChannelType.SMS}";
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
                }
                else
                {
                    await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            LogDispatchNotificationToResident(_logger, package.CourierAwb, resident.FullName ?? "resident", null);
            return true;
        }
    }
}
