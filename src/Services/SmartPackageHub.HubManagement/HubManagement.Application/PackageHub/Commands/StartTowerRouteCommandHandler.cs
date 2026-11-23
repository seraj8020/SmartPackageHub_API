using HubManagement.Application.HubResidents.Services;
using HubManagement.Application.PackageHub.DTOs;
using HubManagement.Application.PackageHub.Services;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.PackageHub.Commands
{
    /// <summary>
    /// Dispatches all shelved packages for a tower (optional floor) so the runner can start handover.
    /// Packages under an active vacation hold stay shelved at the hub.
    /// </summary>
    public sealed class StartTowerRouteCommandHandler
        : CreateCommandHandler<StartTowerRouteCommand, StartTowerRouteResult>
    {
        private static readonly Action<ILogger, Guid, string, int, Exception?> LogRouteStarted =
            LoggerMessage.Define<Guid, string, int>(
                LogLevel.Information,
                new EventId(1, nameof(StartTowerRouteCommandHandler)),
                "Started tower route for society {SocietyId} tower {Tower} ? dispatched {Count} packages");

        private readonly IRepository<Society> _societies;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageCustodyHop> _custodyHops;
        private readonly IRepository<User> _users;
        private readonly IRepository<VacationHold> _vacationHolds;
        private readonly ILogger<StartTowerRouteCommandHandler> _logger;

        public StartTowerRouteCommandHandler(
            IRepository<Society> societies,
            IRepository<Package> packages,
            IRepository<PackageCustodyHop> custodyHops,
            IRepository<User> users,
            IRepository<VacationHold> vacationHolds,
            ILogger<StartTowerRouteCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _societies = societies;
            _packages = packages;
            _custodyHops = custodyHops;
            _users = users;
            _vacationHolds = vacationHolds;
            _logger = logger;
        }

        public override async Task<StartTowerRouteResult> Handle(
            StartTowerRouteCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var tower = request.TowerWing?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(tower))
            {
                return new StartTowerRouteResult(
                    true, false, "Tower is required to start a route.", 0, Array.Empty<Guid>());
            }

            var society = await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            if (society is null)
            {
                return new StartTowerRouteResult(
                    false, false, "Society was not found.", 0, Array.Empty<Guid>());
            }

            var shelved = await _packages.ListAsync(
                package =>
                    package.SocietyId == request.SocietyId &&
                    package.Status == PackageStatus.Shelved,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var candidates = shelved
                .Where(package =>
                    string.Equals(
                        package.TowerWing?.Trim(),
                        tower,
                        StringComparison.OrdinalIgnoreCase))
                .Where(package =>
                    request.Floor is null ||
                    SocietyPackageMapper.TryGetFloor(package.FlatNumber.Value) == request.Floor)
                .OrderBy(package => SocietyPackageMapper.TryGetFloor(package.FlatNumber.Value) ?? int.MaxValue)
                .ThenBy(package => package.FlatNumber.Value, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (candidates.Count == 0)
            {
                var scope = request.Floor is null
                    ? $"Tower {tower}"
                    : $"Tower {tower}, floor {request.Floor}";
                return new StartTowerRouteResult(
                    true,
                    false,
                    $"No shelved packages for {scope}.",
                    0,
                    Array.Empty<Guid>());
            }

            var now = DateTime.UtcNow;
            var routePackages = new List<Package>();
            var heldCount = 0;
            foreach (var package in candidates)
            {
                if (await VacationHoldLookup.IsDoorDeliveryBlockedAsync(_vacationHolds, package, now)
                        .ConfigureAwait(false))
                {
                    heldCount++;
                    continue;
                }

                routePackages.Add(package);
            }

            if (routePackages.Count == 0)
            {
                var scope = request.Floor is null
                    ? $"Tower {tower}"
                    : $"Tower {tower}, floor {request.Floor}";
                return new StartTowerRouteResult(
                    true,
                    false,
                    heldCount > 0
                        ? $"No packages dispatched for {scope}: {heldCount} package(s) are on vacation hold and must stay at the hub."
                        : $"No shelved packages for {scope}.",
                    0,
                    Array.Empty<Guid>());
            }

            var staffNames = new Dictionary<Guid, string?>();
            foreach (var package in routePackages)
            {
                var fromZone = PackageCustodyRecorder.LatestZone(_custodyHops, package.Id)
                    ?? PackageCustodyHopKinds.DefaultHubZone;
                if (!staffNames.TryGetValue(package.StaffUserId, out var staffName))
                {
                    staffName = await PackageCustodyRecorder
                        .ResolveUsernameAsync(_users, package.StaffUserId, cancellationToken)
                        .ConfigureAwait(false);
                    staffNames[package.StaffUserId] = staffName;
                }

                package.DispatchPackage();
                PackageCustodyRecorder.Insert(
                    _custodyHops,
                    package.Id,
                    package.SocietyId,
                    PackageCustodyHopKinds.OutForDoorDelivery,
                    now,
                    fromZone: fromZone,
                    toZone: PackageCustodyRecorder.DoorZone(package.TowerWing, package.FlatNumber.Value),
                    locationLabel: package.FlatNumber.Value,
                    staffUserId: package.StaffUserId,
                    staffUsername: staffName,
                    notes: $"Tower route {tower}");
            }

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);

            var ids = routePackages.Select(package => package.Id).ToList();
            LogRouteStarted(_logger, request.SocietyId, tower, ids.Count, null);

            var floorLabel = request.Floor is null ? "all floors" : $"floor {request.Floor}";
            var heldNote = heldCount > 0
                ? $" {heldCount} package(s) skipped due to vacation hold."
                : string.Empty;
            return new StartTowerRouteResult(
                true,
                true,
                $"Route started for Tower {tower} ({floorLabel}). {ids.Count} package(s) dispatched for handover.{heldNote}",
                ids.Count,
                ids);
        }
    }
}
