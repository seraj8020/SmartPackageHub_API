using HubManagement.Application.PackageHub.DTOs;
using HubManagement.Application.PackageHub.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.PackageHub.Commands
{
    /// <summary>
    /// Resident unavailable at door: staff returns package to hub.
    /// Slot stays reserved (same ShelfId); status returns to Shelved.
    /// </summary>
    public sealed class ReShelveAfterFailedDeliveryCommandHandler
        : CreateCommandHandler<ReShelveAfterFailedDeliveryCommand, PackageCommandResult>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogReShelved =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(ReShelveAfterFailedDeliveryCommandHandler)),
                "Re-shelved package {PackageId} after failed door delivery");

        private readonly IRepository<Package> _packages;
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<PackageCustodyHop> _custodyHops;
        private readonly IRepository<User> _users;
        private readonly ILogger<ReShelveAfterFailedDeliveryCommandHandler> _logger;

        public ReShelveAfterFailedDeliveryCommandHandler(
            IRepository<Package> packages,
            IRepository<Shelf> shelves,
            IRepository<PackageCustodyHop> custodyHops,
            IRepository<User> users,
            ILogger<ReShelveAfterFailedDeliveryCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _shelves = shelves;
            _custodyHops = custodyHops;
            _users = users;
            _logger = logger;
        }

        public override async Task<PackageCommandResult> Handle(
            ReShelveAfterFailedDeliveryCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            if (package is null)
                return new PackageCommandResult(false, false, false, "Package not found.", null);

            if (!PackageStaffGuard.HasSocietyAccess(package, request.StaffSocietyIds))
            {
                return new PackageCommandResult(
                    true, false, false, "Staff is not assigned to this package's society.", null, Forbidden: true);
            }

            if (package.Status != PackageStatus.Dispatched)
            {
                return new PackageCommandResult(
                    true, true, false, "Only dispatched packages can be returned to the shelf after a failed delivery.", null);
            }

            if (package.ShelfId is not Guid shelfId)
            {
                return new PackageCommandResult(
                    true, false, false, "Package has no shelf slot to restore. Assign a slot before re-shelving.", null);
            }

            var shelf = await _shelves.GetByIdAsync(shelfId).ConfigureAwait(false);
            if (shelf is null)
            {
                return new PackageCommandResult(
                    true, false, false, "The assigned shelf slot no longer exists.", null);
            }

            try
            {
                package.ReShelveAfterFailedDelivery();
            }
            catch (InvalidOperationException ex)
            {
                return new PackageCommandResult(true, false, false, ex.Message, null);
            }

            var fromZone = PackageCustodyRecorder.LatestZone(_custodyHops, package.Id)
                ?? PackageCustodyRecorder.DoorZone(package.TowerWing, package.FlatNumber.Value);
            var staffName = await PackageCustodyRecorder
                .ResolveUsernameAsync(_users, package.StaffUserId, cancellationToken)
                .ConfigureAwait(false);

            PackageCustodyRecorder.Insert(
                _custodyHops,
                package.Id,
                package.SocietyId,
                PackageCustodyHopKinds.ReturnedToCage,
                DateTime.UtcNow,
                fromZone: fromZone,
                toZone: PackageCustodyRecorder.ResolveShelfZone(shelf),
                locationLabel: shelf.Code,
                staffUserId: package.StaffUserId,
                staffUsername: staffName,
                notes: "Resident unavailable — returned to hub shelf slot");

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogReShelved(_logger, package.Id, null);

            return new PackageCommandResult(
                true,
                false,
                true,
                $"Package returned to hub slot {shelf.Code}. Slot remains reserved.",
                null);
        }
    }
}
