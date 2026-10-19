using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Intake.Commands
{
    public sealed record DispatchIntakeNotificationCommand(Guid IntakeId) : IUpdateCommand<bool>;

    public sealed class DispatchIntakeNotificationCommandHandler : UpdateCommandHandler<DispatchIntakeNotificationCommand, bool>
    {
        private readonly IRepository<PackageIntake> _intakes;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<OTPLog> _otpLogs;
        private readonly IRepository<Notification> _notifications;
        private readonly ILogger<DispatchIntakeNotificationCommandHandler> _logger;

        public DispatchIntakeNotificationCommandHandler(
            IRepository<PackageIntake> intakes,
            IRepository<Package> packages,
            IRepository<Resident> residents,
            IRepository<Shelf> shelves,
            IRepository<OTPLog> otpLogs,
            IRepository<Notification> notifications,
            ILogger<DispatchIntakeNotificationCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _intakes = intakes;
            _packages = packages;
            _residents = residents;
            _shelves = shelves;
            _otpLogs = otpLogs;
            _notifications = notifications;
            _logger = logger;
        }

        public override async Task<bool> Handle(DispatchIntakeNotificationCommand request, CancellationToken cancellationToken)
        {
            var intake = await _intakes.GetByIdAsync(request.IntakeId);
            if (intake == null)
                return false;

            var package = await _packages.GetByIdAsync(intake.PackageId);
            if (package == null)
                return false;

            var alreadySent = _notifications.GetAll().Any(notification =>
                notification.PackageId == package.Id && notification.Content.TemplateName == "IntakeReceived");
            if (alreadySent)
                return true;

            var resident = package.ResidentId is Guid residentId
                ? await _residents.GetByIdAsync(residentId)
                : null;
            var shelfCode = package.ShelfId is Guid shelfId
                ? (await _shelves.GetByIdAsync(shelfId))?.Code
                : null;
            var otp = _otpLogs.GetAll()
                .Where(log => log.PackageId == package.Id && log.Purpose == "Pickup")
                .ToList()
                .OrderByDescending(log => log.Expiry?.Value)
                .Select(log => log.Token?.Value)
                .FirstOrDefault();

            var unit = string.IsNullOrWhiteSpace(package.TowerWing) ? package.FlatNumber.Value : $"{package.TowerWing}-{package.FlatNumber.Value}";
            var phone = resident?.PhoneNumber;
            var body =
                $"Package received at Smart Package Hub. " +
                $"Courier: {package.CourierPartner ?? "Courier"}. " +
                $"AWB: {package.CourierAwb}. " +
                $"Flat: {unit}. " +
                $"Shelf: {shelfCode ?? "unassigned"}. " +
                $"Pickup OTP: {otp ?? "pending"}. " +
                (phone is { Length: > 0 } ? $"WhatsApp: {phone}." : "WhatsApp: no phone on file.");

            if (resident == null)
            {
                _logger.LogInformation("Intake {Awb} has no resident yet. Notice: {Body}", package.CourierAwb, body);
                return true;
            }

            var push = Notification.Create(package.SocietyId, resident.Id, package.Id, NotificationChannel.Create(ChannelType.Push, resident.Id.ToString()), MessageContent.Create("IntakeReceived", body));
            push.MarkAsSent();
            _notifications.Insert(push);

            if (phone is { Length: > 0 })
            {
                var sms = Notification.Create(package.SocietyId, resident.Id, package.Id, NotificationChannel.Create(ChannelType.SMS, phone), MessageContent.Create("IntakeReceived", body));
                sms.MarkAsSent();
                _notifications.Insert(sms);
            }

            await UnitOfWork!.CommitAsync(cancellationToken);
            _logger.LogInformation("Dispatched push + WhatsApp intake notice for {Awb} to {Resident}", package.CourierAwb, resident.FullName ?? "resident");
            return true;
        }
    }
}
