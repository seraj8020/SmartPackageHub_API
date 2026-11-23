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
    /// Logs an arrived-at-tower/floor custody hop for dispatched door-route packages.
    /// Does not change package status. Hub counter is intentionally out of scope.
    /// </summary>
    public sealed class MarkTowerFloorArrivalCommandHandler
        : CreateCommandHandler<MarkTowerFloorArrivalCommand, MarkTowerFloorArrivalResult>
    {
        private static readonly Action<ILogger, Guid, string, int, Exception?> LogArrival =
            LoggerMessage.Define<Guid, string, int>(
                LogLevel.Information,
                new EventId(1, nameof(MarkTowerFloorArrivalCommandHandler)),
                "Marked tower/floor arrival for society {SocietyId} zone {Zone} on {Count} packages");

        private readonly IRepository<Society> _societies;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageCustodyHop> _custodyHops;
        private readonly IRepository<User> _users;
        private readonly ILogger<MarkTowerFloorArrivalCommandHandler> _logger;

        public MarkTowerFloorArrivalCommandHandler(
            IRepository<Society> societies,
            IRepository<Package> packages,
            IRepository<PackageCustodyHop> custodyHops,
            IRepository<User> users,
            ILogger<MarkTowerFloorArrivalCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _societies = societies;
            _packages = packages;
            _custodyHops = custodyHops;
            _users = users;
            _logger = logger;
        }

        public override async Task<MarkTowerFloorArrivalResult> Handle(
            MarkTowerFloorArrivalCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var tower = request.TowerWing?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(tower))
            {
                return new MarkTowerFloorArrivalResult(
                    true, false, "Tower is required to mark arrival.", 0, Array.Empty<Guid>());
            }

            var society = await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            if (society is null)
            {
                return new MarkTowerFloorArrivalResult(
                    false, false, "Society was not found.", 0, Array.Empty<Guid>());
            }

            var dispatched = await _packages.ListAsync(
                    package =>
                        package.SocietyId == request.SocietyId &&
                        package.Status == PackageStatus.Dispatched,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var routePackages = dispatched
                .Where(package =>
                    string.Equals(
                        package.TowerWing?.Trim(),
                        tower,
                        StringComparison.OrdinalIgnoreCase))
                .Where(package =>
                    request.Floor is null ||
                    SocietyPackageMapper.TryGetFloor(package.FlatNumber.Value) == request.Floor)
                .ToList();

            if (routePackages.Count == 0)
            {
                var scope = request.Floor is null
                    ? $"Tower {tower}"
                    : $"Tower {tower}, floor {request.Floor}";
                return new MarkTowerFloorArrivalResult(
                    true,
                    false,
                    $"No dispatched packages for {scope}. Start the route first.",
                    0,
                    Array.Empty<Guid>());
            }

            var toZone = PackageCustodyRecorder.TowerFloorZone(tower, request.Floor);
            var now = DateTime.UtcNow;
            var staffNames = new Dictionary<Guid, string?>();
            var loggedIds = new List<Guid>();
            var locationLabel = request.Floor is int floorNum ? $"Floor {floorNum}" : tower;
            var notes = request.Floor is null
                ? $"Arrived at tower {tower}"
                : $"Arrived at tower {tower}, floor {request.Floor}";

            foreach (var package in routePackages)
            {
                var fromZone = PackageCustodyRecorder.LatestZone(_custodyHops, package.Id)
                    ?? PackageCustodyHopKinds.DefaultHubZone;
                if (string.Equals(fromZone, toZone, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!staffNames.TryGetValue(package.StaffUserId, out var staffName))
                {
                    staffName = await PackageCustodyRecorder
                        .ResolveUsernameAsync(_users, package.StaffUserId, cancellationToken)
                        .ConfigureAwait(false);
                    staffNames[package.StaffUserId] = staffName;
                }

                PackageCustodyRecorder.Insert(
                    _custodyHops,
                    package.Id,
                    package.SocietyId,
                    PackageCustodyHopKinds.ArrivedAtTowerFloor,
                    now,
                    fromZone: fromZone,
                    toZone: toZone,
                    locationLabel: locationLabel,
                    staffUserId: package.StaffUserId,
                    staffUsername: staffName,
                    notes: notes);
                loggedIds.Add(package.Id);
            }

            if (loggedIds.Count == 0)
            {
                return new MarkTowerFloorArrivalResult(
                    true,
                    true,
                    $"Arrival at {toZone} was already logged for this batch.",
                    0,
                    Array.Empty<Guid>());
            }

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogArrival(_logger, request.SocietyId, toZone, loggedIds.Count, null);

            return new MarkTowerFloorArrivalResult(
                true,
                true,
                $"Logged arrival at {toZone} for {loggedIds.Count} package(s).",
                loggedIds.Count,
                loggedIds);
        }
    }
}