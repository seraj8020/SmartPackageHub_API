using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Commands
{
    public sealed class CreateShelfCommandHandler : CreateCommandHandler<CreateShelfCommand, ShelfCommandResult>
    {
        private static readonly Action<ILogger, Guid, Guid, Exception?> LogCreatedShelf =
            LoggerMessage.Define<Guid, Guid>(
                LogLevel.Information,
                new EventId(1, nameof(CreateShelfCommandHandler)),
                "Created shelf {ShelfId} for society {SocietyId}");

        private readonly IRepository<Society> _societies;
        private readonly IRepository<Shelf> _shelves;
        private readonly ILogger<CreateShelfCommandHandler> _logger;

        public CreateShelfCommandHandler(
            IRepository<Society> societies,
            IRepository<Shelf> shelves,
            ILogger<CreateShelfCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _societies = societies;
            _shelves = shelves;
            _logger = logger;
        }

        public override async Task<ShelfCommandResult> Handle(CreateShelfCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var society = await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            if (society == null)
                return new ShelfCommandResult(false, false, "Society was not found.", null);

            var slotCode = ShelfCommandHelper.ResolveSlotCode(
                request.Code, request.Zone, request.Aisle, request.Tier, request.TowerOrWing);

            var fieldError = ShelfCommandHelper.ValidateFields(slotCode, request.TowerOrWing, request.Capacity);
            if (fieldError != null)
                return new ShelfCommandResult(true, false, fieldError, null);

            var locationResult = ShelfCommandHelper.TryCreateLocation(request.Zone, request.Aisle, request.Tier);
            if (!locationResult.Succeeded)
                return new ShelfCommandResult(true, false, locationResult.Message, null);

            var existing = await _shelves.ListAsync(
                shelf => shelf.SocietyId == request.SocietyId,
                noTracking: true,
                cancellationToken).ConfigureAwait(false);

            if (existing.Any(shelf => ShelfCommandHelper.CodesMatch(shelf.Code, slotCode)))
            {
                return new ShelfCommandResult(
                    true,
                    false,
                    $"Slot code '{slotCode}' already exists in this society.",
                    null);
            }

            // True per-slot addresses use capacity 1 so one package ↔ one A-12-3.
            var capacity = request.Capacity < 1 ? 1 : request.Capacity;

            var shelf = Shelf.Create(
                request.SocietyId,
                slotCode,
                request.TowerOrWing.Trim(),
                request.IsColdStorage,
                capacity,
                locationResult.Location!);

            _shelves.Insert(shelf);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogCreatedShelf(_logger, shelf.Id, society.Id, null);
            return new ShelfCommandResult(true, true, $"Slot {slotCode} created.", ShelfCommandHelper.ToDto(shelf));
        }
    }
}
