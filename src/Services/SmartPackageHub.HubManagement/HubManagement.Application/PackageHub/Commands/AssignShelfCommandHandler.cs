using System.Globalization;
using System.Security.Cryptography;
using HubManagement.Application.DeliverySlots;
using SmartPackageHub.Common.Services.FeatureFlags.Abstractions;
using SmartPackageHub.Common.Services.FeatureFlags.Options;
using SmartPackageHub.Common.Services.FeatureFlags.Services;
using HubManagement.Application.Intake;
using HubManagement.Application.Intake.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using HubManagement.Application.Intake.Converters;
using HubManagement.Application.Intake.Helpers;
using HubManagement.Application.Intake.Services;
using HubManagement.Application.DeliverySlots.Helpers;
using HubManagement.Application.DeliverySlots.Services;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed class AssignShelfCommandHandler : CreateCommandHandler<AssignShelfCommand, AssignShelfResult>
    {
        private static readonly TimeSpan PickupOtpLifetime = TimeSpan.FromDays(7);

        private static readonly Action<ILogger, string, Guid, Exception?> LogShelfAssigned =
            LoggerMessage.Define<string, Guid>(
                LogLevel.Information,
                new EventId(1, nameof(AssignShelfCommandHandler)),
                "Assigned shelf {ShelfCode} to package {PackageId}");

        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageIntake> _intakes;
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<OtpLog> _otpLogs;
        private readonly IRepository<DeliverySlot> _deliverySlots;
        private readonly IRepository<Notification> _notifications;
        private readonly IRepository<PackageImage> _images;
        private readonly IntakeNotificationChannel _queue;
        private readonly IFeatureFlags _featureFlags;
        private readonly ILogger<AssignShelfCommandHandler> _logger;

        public AssignShelfCommandHandler(
            IRepository<Package> packages,
            IRepository<PackageIntake> intakes,
            IRepository<Shelf> shelves,
            IRepository<Resident> residents,
            IRepository<OtpLog> otpLogs,
            IRepository<DeliverySlot> deliverySlots,
            IRepository<Notification> notifications,
            IRepository<PackageImage> images,
            IntakeNotificationChannel queue,
            IFeatureFlags featureFlags,
            ILogger<AssignShelfCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _intakes = intakes;
            _shelves = shelves;
            _residents = residents;
            _otpLogs = otpLogs;
            _deliverySlots = deliverySlots;
            _notifications = notifications;
            _images = images;
            _queue = queue;
            _featureFlags = featureFlags;
            _logger = logger;
        }

        public override async Task<AssignShelfResult> Handle(AssignShelfCommand command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            var package = await _packages.GetByIdAsync(command.PackageId).ConfigureAwait(false);
            if (package == null)
            {
                return new AssignShelfResult { Found = false, Message = "Package was not found." };
            }

            if (package.Status != PackageStatus.Received)
            {
                return new AssignShelfResult
                {
                    Found = true,
                    Conflict = true,
                    Succeeded = false,
                    Message = $"Only received packages can be shelved (current status: {package.Status}).",
                };
            }

            // When CashOnDelivery feature is off, skip COD approval gates so staff can still shelf.
            if (_featureFlags.CashOnDelivery)
            {
                if (package.IsCod && package.CodApprovalStatus == CodApprovalStatus.Pending)
                {
                    return new AssignShelfResult
                    {
                        Found = true,
                        Succeeded = false,
                        Message = "COD package is waiting for resident approval before it can be shelved.",
                    };
                }

                if (package.IsCod && package.CodApprovalStatus == CodApprovalStatus.Rejected)
                {
                    return new AssignShelfResult
                    {
                        Found = true,
                        Succeeded = false,
                        Message = "COD was rejected by the resident. Package cannot be shelved.",
                    };
                }

                if (package.IsCod && package.CodApprovalStatus != CodApprovalStatus.Approved)
                {
                    return new AssignShelfResult
                    {
                        Found = true,
                        Succeeded = false,
                        Message = "COD package must be approved before shelving.",
                    };
                }
            }

            var shelfCode = IntakeText.Clean(command.ShelfCode, 50);
            if (shelfCode == null)
            {
                return new AssignShelfResult
                {
                    Found = true,
                    Succeeded = false,
                    Message = "Shelf code is required.",
                };
            }

            var shelf = ResolveOrCreateShelf(package.SocietyId, shelfCode, command.IsColdStorage || package.IsColdStorage);
            package.AssignShelf(shelf.Id);
            package.ShelfPackage();

            string? otp = null;
            Resident? resident = null;
            if (package.ResidentId is Guid residentId)
            {
                resident = await _residents.GetByIdAsync(residentId).ConfigureAwait(false);
                if (resident != null)
                {
                    otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
                    _otpLogs.Insert(OtpLog.Generate(resident.SocietyId, resident.Id, package.Id, otp, PickupOtpLifetime, "Pickup"));
                    AssignDefaultDeliverySlot(package, resident);
                }
            }

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);

            var intake = _intakes.FetchMulti(row => row.PackageId == package.Id).FirstOrDefault();
            if (intake != null)
                _queue.Enqueue(intake.Id);

            LogShelfAssigned(_logger, shelf.Code, package.Id, null);

            return new AssignShelfResult
            {
                Found = true,
                Succeeded = true,
                Message = "Package shelved. Pickup OTP queued for the resident.",
                Receipt = await MapReceiptAsync(intake, package, shelf.Code, otp, resident).ConfigureAwait(false),
            };
        }

        private Shelf ResolveOrCreateShelf(Guid societyId, string code, bool isColdStorage)
        {
            // Load society shelves first ? string.Equals(..., OrdinalIgnoreCase) is not EF-translatable.
            var societyShelves = _shelves.GetAll()
                .Where(item => item.SocietyId == societyId)
                .AsEnumerable()
                .ToList();
            var existing = societyShelves.FirstOrDefault(item =>
                string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                return existing;

            var aisle = "A";
            var tier = 1;
            var parts = code.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2)
            {
                var aisleToken = parts[^2];
                if (aisleToken.Length > 0)
                    aisle = aisleToken.Length == 1 ? aisleToken.ToUpperInvariant() : aisleToken;
                if (int.TryParse(parts[^1], out var parsedTier) && parsedTier > 0)
                    tier = parsedTier;
            }
            else if (parts.Length == 1 && parts[0].Length > 0)
            {
                aisle = parts[0].Substring(0, 1).ToUpperInvariant();
            }

            var location = HubManagement.Core.Hubs.ValueObjects.ShelfLocation.Create(
                isColdStorage ? "Cold" : "Hub",
                aisle,
                tier);

            var created = Shelf.Create(societyId, code, aisle, isColdStorage, capacity: 40, location);
            _shelves.Insert(created);
            return created;
        }

        private void AssignDefaultDeliverySlot(Package package, Resident resident)
        {
            var activeByPackage = _deliverySlots
                .FetchMulti(slot =>
                    slot.PackageId == package.Id &&
                    slot.Status != DeliverySlotWindows.StatusCancelled)
                .GroupBy(slot => slot.PackageId)
                .ToDictionary(group => group.Key, group => group.First());

            DefaultDeliverySlotService.EnsureDefaultForPackage(
                _deliverySlots,
                package,
                resident.Id,
                DateTime.UtcNow,
                activeByPackage);
        }

        private Task<ConfirmIntakeResponse> MapReceiptAsync(
            PackageIntake? intake,
            Package package,
            string shelfCode,
            string? otp,
            Resident? resident)
        {
            ResidentMatchDto? residentDto = resident != null ? ResidentMatcher.ToDto(resident) : null;
            var notified = _notifications.GetAll().Any(notification =>
                notification.PackageId == package.Id && notification.Content.TemplateName == "IntakeReceived");

            return Task.FromResult(new ConfirmIntakeResponse
            {
                IntakeId = intake?.Id ?? Guid.Empty,
                PackageId = package.Id,
                PkgNumber = package.PkgNumber,
                Awb = package.CourierAwb,
                Courier = package.CourierPartner,
                ShelfCode = shelfCode,
                Status = package.Status.ToString(),
                IsCod = package.IsCod,
                CodAmount = package.CodAmount,
                CodApprovalStatus = package.CodApprovalStatus.ToString(),
                PickupOtp = otp,
                PickupQrPayload = string.IsNullOrEmpty(otp) ? string.Empty : $"SPH|{package.PkgNumber}|{otp}",
                NotificationStatus = notified ? "Dispatched" : "Queued",
                MatchStatus = intake?.MatchStatus ?? "missing",
                PhotoStored = intake != null && _images.GetAll().Any(image => image.IntakeId == intake.Id),
                Resident = residentDto,
            });
        }
    }
}
