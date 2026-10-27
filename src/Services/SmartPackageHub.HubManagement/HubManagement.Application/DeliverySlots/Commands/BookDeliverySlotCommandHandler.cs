using HubManagement.Application.DeliverySlots.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using HubManagement.Application.DeliverySlots.Helpers;

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

        private readonly IRepository<Package> _packages;
        private readonly IRepository<DeliverySlot> _slots;
        private readonly IRepository<Resident> _residents;
        private readonly ILogger<BookDeliverySlotCommandHandler> _logger;

        public BookDeliverySlotCommandHandler(
            IRepository<Package> packages,
            IRepository<DeliverySlot> slots,
            IRepository<Resident> residents,
            ILogger<BookDeliverySlotCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _slots = slots;
            _residents = residents;
            _logger = logger;
        }

        public override async Task<BookDeliverySlotResult> Handle(
            BookDeliverySlotCommand command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);
            var request = command.Request;

            if (!DeliverySlotWindows.TryResolveWindow(request.WindowStart, request.WindowEnd, out _))
            {
                return new BookDeliverySlotResult
                {
                    Found = true,
                    Succeeded = false,
                    Message = "Choose a valid delivery window (9–12, 12–3, 3–6, or 6–9).",
                };
            }

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
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

            if (package.SocietyId != request.SocietyId ||
                package.ResidentId != request.ResidentId)
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

            // Resident selection overrides the system default 6–9 window.
            if (activeForPackage != null && DeliverySlotWindows.IsSystemDefault(activeForPackage.Status))
            {
                activeForPackage.Cancel();
            }

            var slot = DeliverySlot.Create(
                request.SocietyId,
                request.ResidentId,
                request.PackageId,
                request.WindowStart,
                request.WindowEnd,
                DeliverySlotWindows.StatusBooked,
                request.Notes);

            _slots.Insert(slot);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogSlotBooked(_logger, slot.Id, package.Id, null);

            return new BookDeliverySlotResult
            {
                Found = true,
                Succeeded = true,
                Message = "Delivery slot booked.",
                Slot = Map(slot, package),
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
