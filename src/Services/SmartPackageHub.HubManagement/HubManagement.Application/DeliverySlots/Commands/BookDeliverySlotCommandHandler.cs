using HubManagement.Application.DeliverySlots.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using HubManagement.Application.DeliverySlots.Helpers;
using HubManagement.Application.HubResidents.Services;

namespace HubManagement.Application.DeliverySlots.Commands
{
    public sealed class BookDeliverySlotCommandHandler
        : CreateCommandHandler<BookDeliverySlotCommand, BookDeliverySlotResult>
    {
        private static readonly Action<ILogger, Guid, Guid, Exception?> LogSlotBooked =
            LoggerMessage.Define<Guid, Guid>(
                LogLevel.Information,
                new EventId(1, nameof(BookDeliverySlotCommandHandler)),
                "Booked delivery slot {SlotId} for package {PackageId}");

        private static readonly Action<ILogger, Guid, string, Exception?> LogPreferenceSaved =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Information,
                new EventId(2, nameof(BookDeliverySlotCommandHandler)),
                "Saved delivery preference {Method} for package {PackageId}");

        private readonly IRepository<Package> _packages;
        private readonly IRepository<DeliverySlot> _slots;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<VacationHold> _vacationHolds;
        private readonly ILogger<BookDeliverySlotCommandHandler> _logger;

        public BookDeliverySlotCommandHandler(
            IRepository<Package> packages,
            IRepository<DeliverySlot> slots,
            IRepository<Resident> residents,
            IRepository<VacationHold> vacationHolds,
            ILogger<BookDeliverySlotCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _slots = slots;
            _residents = residents;
            _vacationHolds = vacationHolds;
            _logger = logger;
        }

        public override async Task<BookDeliverySlotResult> Handle(
            BookDeliverySlotCommand command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            var request = command.Request ?? throw new ArgumentNullException(nameof(command));
            var method = NormalizeMethod(request.PreferredDeliveryMethod);

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            var guard = await ValidatePackageAndResidentAsync(package, request).ConfigureAwait(false);
            if (guard is not null)
                return guard;

            if (string.Equals(method, "CounterPickup", StringComparison.Ordinal))
                return await SaveCounterPickupAsync(package!, cancellationToken).ConfigureAwait(false);

            return await SaveDoorDeliveryAsync(package!, request, cancellationToken).ConfigureAwait(false);
        }

        private async Task<BookDeliverySlotResult> SaveCounterPickupAsync(
            Package package,
            CancellationToken cancellationToken)
        {
            CancelActiveSlots(package.Id);
            package.SetPreferredDeliveryMethod("CounterPickup");
            _packages.Update(package);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogPreferenceSaved(_logger, package.Id, "CounterPickup", null);
            return new BookDeliverySlotResult
            {
                Found = true,
                Succeeded = true,
                Message = "Counter pickup selected. Package will stay at the hub shelf.",
            };
        }

        private async Task<BookDeliverySlotResult> SaveDoorDeliveryAsync(
            Package package,
            BookDeliverySlotRequest request,
            CancellationToken cancellationToken)
        {
            if (!DeliverySlotWindows.TryResolveWindow(request.WindowStart, request.WindowEnd, out _))
            {
                return new BookDeliverySlotResult
                {
                    Found = true,
                    Succeeded = false,
                    Message = "Choose a valid delivery window (9-12, 12-3, or 3-6).",
                };
            }

            // 6–9 is the system default already set at shelving — residents change away from it.
            if (request.WindowStart.TimeOfDay == DeliverySlotWindows.DefaultWindow.Start &&
                request.WindowEnd.TimeOfDay == DeliverySlotWindows.DefaultWindow.End)
            {
                return new BookDeliverySlotResult
                {
                    Found = true,
                    Succeeded = false,
                    Message = "6–9 is already the default door window. Choose 9–12, 12–3, or 3–6 to change it.",
                };
            }

            var vacationBlock = await TryBlockForVacationHoldAsync(package, request).ConfigureAwait(false);
            if (vacationBlock is not null)
                return vacationBlock;

            var activeForPackage = _slots
                .FetchMulti(slot =>
                    slot.PackageId == request.PackageId &&
                    slot.Status != DeliverySlotWindows.StatusCancelled)
                .FirstOrDefault();

            if (activeForPackage != null && !DeliverySlotWindows.IsSystemDefault(activeForPackage.Status))
            {
                return new BookDeliverySlotResult
                {
                    Found = true,
                    Conflict = true,
                    Succeeded = false,
                    Message = "A delivery slot is already booked for this package.",
                    Slot = Map(activeForPackage, package),
                };
            }

            var dayStart = request.WindowStart.Date;
            var dayEnd = dayStart.AddDays(1);
            var bookedInWindow = _slots
                .FetchMulti(slot =>
                    slot.SocietyId == request.SocietyId &&
                    slot.Status != DeliverySlotWindows.StatusCancelled &&
                    !DeliverySlotWindows.IsSystemDefault(slot.Status) &&
                    slot.WindowStart >= dayStart &&
                    slot.WindowStart < dayEnd &&
                    slot.WindowStart == request.WindowStart &&
                    slot.WindowEnd == request.WindowEnd)
                .Count(slot => activeForPackage == null || slot.Id != activeForPackage.Id);

            if (bookedInWindow >= DeliverySlotWindows.CapacityPerWindow)
            {
                return new BookDeliverySlotResult
                {
                    Found = true,
                    Conflict = true,
                    Succeeded = false,
                    Message = "This delivery window is full.",
                };
            }

            if (activeForPackage != null && DeliverySlotWindows.IsSystemDefault(activeForPackage.Status))
                activeForPackage.Cancel();

            var slot = DeliverySlot.Create(
                request.SocietyId,
                request.ResidentId,
                request.PackageId,
                request.WindowStart,
                request.WindowEnd,
                DeliverySlotWindows.StatusBooked,
                request.Notes);

            package.SetPreferredDeliveryMethod("DoorDelivery");
            _packages.Update(package);
            _slots.Insert(slot);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogSlotBooked(_logger, slot.Id, package.Id, null);

            return new BookDeliverySlotResult
            {
                Found = true,
                Succeeded = true,
                Message = "Door delivery slot booked.",
                Slot = Map(slot, package),
            };
        }

        private async Task<BookDeliverySlotResult?> ValidatePackageAndResidentAsync(
            Package? package,
            BookDeliverySlotRequest request)
        {
            if (package == null)
            {
                return new BookDeliverySlotResult
                {
                    Found = false,
                    Succeeded = false,
                    Message = "Package not found.",
                };
            }

            if (package.Status == PackageStatus.Delivered)
            {
                return new BookDeliverySlotResult
                {
                    Found = true,
                    Conflict = true,
                    Succeeded = false,
                    Message = "Package is already delivered.",
                };
            }

            if (package.SocietyId != request.SocietyId || package.ResidentId != request.ResidentId)
            {
                return new BookDeliverySlotResult
                {
                    Found = true,
                    Succeeded = false,
                    Message = "Package does not match the resident or society.",
                };
            }

            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident == null)
            {
                return new BookDeliverySlotResult
                {
                    Found = false,
                    Succeeded = false,
                    Message = "Resident not found.",
                };
            }

            return null;
        }

        private void CancelActiveSlots(Guid packageId)
        {
            var active = _slots
                .FetchMulti(slot =>
                    slot.PackageId == packageId &&
                    slot.Status != DeliverySlotWindows.StatusCancelled)
                .ToList();
            foreach (var slot in active)
                slot.Cancel();
        }

        private static string NormalizeMethod(string? method)
        {
            if (string.IsNullOrWhiteSpace(method))
                return "DoorDelivery";
            if (string.Equals(method, "CounterPickup", StringComparison.OrdinalIgnoreCase))
                return "CounterPickup";
            return "DoorDelivery";
        }

        private async Task<BookDeliverySlotResult?> TryBlockForVacationHoldAsync(
            Package package,
            BookDeliverySlotRequest request)
        {
            var coveringHold = await VacationHoldLookup.FindCoveringAsync(
                _vacationHolds,
                request.ResidentId,
                request.WindowStart.Date).ConfigureAwait(false);
            if (coveringHold is null && !package.VacationHoldId.HasValue)
                return null;

            return new BookDeliverySlotResult
            {
                Found = true,
                Succeeded = false,
                Message = "Door delivery is paused while your vacation hold is active. Choose counter pickup or wait until the hold ends.",
            };
        }

        private static DeliverySlotDto Map(DeliverySlot slot, Package package) =>
            new()
            {
                Id = slot.Id,
                SocietyId = slot.SocietyId,
                ResidentId = slot.ResidentId,
                PackageId = slot.PackageId,
                WindowStart = slot.WindowStart,
                WindowEnd = slot.WindowEnd,
                Status = slot.Status,
                Notes = slot.Notes,
                PkgNumber = package.PkgNumber,
                Courier = package.CourierPartner,
            };
    }
}
