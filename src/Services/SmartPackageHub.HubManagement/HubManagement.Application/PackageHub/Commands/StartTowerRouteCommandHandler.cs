using HubManagement.Application.PackageHub.DTOs;
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
    /// </summary>
    public sealed class StartTowerRouteCommandHandler
        : CreateCommandHandler<StartTowerRouteCommand, StartTowerRouteResult>
    {
        private static readonly Action<ILogger, Guid, string, int, Exception?> LogRouteStarted =
            LoggerMessage.Define<Guid, string, int>(
                LogLevel.Information,
                new EventId(1, nameof(StartTowerRouteCommandHandler)),
                "Started tower route for society {SocietyId} tower {Tower} — dispatched {Count} packages");

        private readonly IRepository<Society> _societies;
        private readonly IRepository<Package> _packages;
        private readonly ILogger<StartTowerRouteCommandHandler> _logger;

        public StartTowerRouteCommandHandler(
            IRepository<Society> societies,
            IRepository<Package> packages,
            ILogger<StartTowerRouteCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _societies = societies;
            _packages = packages;
            _logger = logger;
        }

        public override async Task<StartTowerRouteResult> Handle(
            StartTowerRouteCommand command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            var tower = command.TowerWing?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(tower))
            {
                return new StartTowerRouteResult(
                    true, false, "Tower is required to start a route.", 0, Array.Empty<Guid>());
            }

            var society = await _societies.GetByIdAsync(command.SocietyId).ConfigureAwait(false);
            if (society is null)
            {
                return new StartTowerRouteResult(
                    false, false, "Society was not found.", 0, Array.Empty<Guid>());
            }

            var shelved = await _packages.ListAsync(
                package =>
                    package.SocietyId == command.SocietyId &&
                    package.Status == PackageStatus.Shelved,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var routePackages = shelved
                .Where(package =>
                    string.Equals(
                        package.TowerWing?.Trim(),
                        tower,
                        StringComparison.OrdinalIgnoreCase))
                .Where(package =>
                    command.Floor is null ||
                    SocietyPackageMapper.TryGetFloor(package.FlatNumber.Value) == command.Floor)
                .OrderBy(package => SocietyPackageMapper.TryGetFloor(package.FlatNumber.Value) ?? int.MaxValue)
                .ThenBy(package => package.FlatNumber.Value, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (routePackages.Count == 0)
            {
                var scope = command.Floor is null
                    ? $"Tower {tower}"
                    : $"Tower {tower}, floor {command.Floor}";
                return new StartTowerRouteResult(
                    true,
                    false,
                    $"No shelved packages for {scope}.",
                    0,
                    Array.Empty<Guid>());
            }

            foreach (var package in routePackages)
                package.DispatchPackage();

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);

            var ids = routePackages.Select(package => package.Id).ToList();
            LogRouteStarted(_logger, command.SocietyId, tower, ids.Count, null);

            var floorLabel = command.Floor is null ? "all floors" : $"floor {command.Floor}";
            return new StartTowerRouteResult(
                true,
                true,
                $"Route started for Tower {tower} ({floorLabel}). {ids.Count} package(s) dispatched for handover.",
                ids.Count,
                ids);
        }
    }
}
