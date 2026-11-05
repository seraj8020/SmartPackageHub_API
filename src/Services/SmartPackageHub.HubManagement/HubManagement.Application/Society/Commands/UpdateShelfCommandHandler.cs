using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Commands
{
    public sealed class UpdateShelfCommandHandler : UpdateCommandHandler<UpdateShelfCommand, ShelfCommandResult>
    {
        private static readonly Action<ILogger, Guid, Guid, Exception?> LogUpdatedShelf =
            LoggerMessage.Define<Guid, Guid>(
                LogLevel.Information,
                new EventId(1, nameof(UpdateShelfCommandHandler)),
                "Updated shelf {ShelfId} for society {SocietyId}");

        private readonly IRepository<Society> _societies;
        private readonly IRepository<Shelf> _shelves;
        private readonly ILogger<UpdateShelfCommandHandler> _logger;

        public UpdateShelfCommandHandler(
            IRepository<Society> societies,
            IRepository<Shelf> shelves,
            ILogger<UpdateShelfCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _societies = societies;
            _shelves = shelves;
            _logger = logger;
        }

        public override async Task<ShelfCommandResult> Handle(UpdateShelfCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var society = await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            if (society == null)
                return new ShelfCommandResult(false, false, "Society was not found.", null);

            var shelf = await _shelves.GetByIdAsync(request.ShelfId).ConfigureAwait(false);
            if (shelf == null || shelf.SocietyId != request.SocietyId)
                return new ShelfCommandResult(false, false, "Shelf was not found.", null);

            var fieldError = ShelfCommandHelper.ValidateFields(request.Code, request.TowerOrWing, request.Capacity);
            if (fieldError != null)
                return new ShelfCommandResult(true, false, fieldError, null);

            var locationResult = ShelfCommandHelper.TryCreateLocation(request.Zone, request.Aisle, request.Tier);
            if (!locationResult.Succeeded)
                return new ShelfCommandResult(true, false, locationResult.Message, null);

            var siblings = await _shelves.ListAsync(
                item => item.SocietyId == request.SocietyId && item.Id != request.ShelfId,
                noTracking: true,
                cancellationToken).ConfigureAwait(false);

            if (siblings.Any(item => ShelfCommandHelper.CodesMatch(item.Code, request.Code)))
            {
                return new ShelfCommandResult(
                    true,
                    false,
                    $"Shelf code '{request.Code.Trim()}' already exists in this society.",
                    null);
            }

            shelf.Update(
                request.Code.Trim(),
                request.TowerOrWing.Trim(),
                request.IsColdStorage,
                request.Capacity,
                locationResult.Location!);

            _shelves.Update(shelf);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogUpdatedShelf(_logger, shelf.Id, society.Id, null);
            return new ShelfCommandResult(true, true, "Shelf updated.", ShelfCommandHelper.ToDto(shelf));
        }
    }
}
