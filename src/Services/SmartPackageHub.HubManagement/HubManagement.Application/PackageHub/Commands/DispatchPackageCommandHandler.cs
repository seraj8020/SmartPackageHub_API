using HubManagement.Application.HubResidents.Services;
using HubManagement.Application.PackageHub.DTOs;
using HubManagement.Application.PackageHub.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
// PackageHoldLookup + AuthorizedCollector
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed class DispatchPackageCommandHandler : CreateCommandHandler<DispatchPackageCommand, PackageCommandResult>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogPackageDispatched =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(DispatchPackageCommandHandler)),
                "Dispatched package {PackageId}");
        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageCustodyHop> _custodyHops;
        private readonly IRepository<User> _users;
        private readonly IRepository<VacationHold> _vacationHolds;
        private readonly IRepository<AuthorizedCollector> _collectors;
        private readonly ILogger<DispatchPackageCommandHandler> _logger;

        public DispatchPackageCommandHandler(
            IRepository<Package> packages,
            IRepository<PackageCustodyHop> custodyHops,
            IRepository<User> users,
            IRepository<VacationHold> vacationHolds,
            IRepository<AuthorizedCollector> collectors,
            ILogger<DispatchPackageCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _custodyHops = custodyHops;
            _users = users;
            _vacationHolds = vacationHolds;
            _collectors = collectors;
            _logger = logger;
        }

        public override async Task<PackageCommandResult> Handle(DispatchPackageCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var package = await _packages.GetByIdAsync(request.packageId).ConfigureAwait(false);
            if (package == null)
                return new PackageCommandResult(false, false, false, "Package not found.", null);

            if (package.Status != PackageStatus.Shelved)
                return new PackageCommandResult(true, true, false, "Only shelved packages can be dispatched.", null);

            var dispatchBlock = await PackageHoldLookup.GetDispatchBlockMessageAsync(
                    _vacationHolds,
                    _collectors,
                    package,
                    DateTime.UtcNow)
                .ConfigureAwait(false);
            if (dispatchBlock is not null)
            {
                return new PackageCommandResult(
                    true,
                    false,
                    false,
                    dispatchBlock,
                    null);
            }

            var fromZone = PackageCustodyRecorder.LatestZone(_custodyHops, package.Id)
                ?? PackageCustodyHopKinds.DefaultHubZone;
            var staffName = await PackageCustodyRecorder
                .ResolveUsernameAsync(_users, package.StaffUserId, cancellationToken)
                .ConfigureAwait(false);

            package.DispatchPackage();
            PackageCustodyRecorder.Insert(
                _custodyHops,
                package.Id,
                package.SocietyId,
                PackageCustodyHopKinds.OutForDoorDelivery,
                DateTime.UtcNow,
                fromZone: fromZone,
                toZone: PackageCustodyRecorder.DoorZone(package.TowerWing, package.FlatNumber.Value),
                locationLabel: package.FlatNumber.Value,
                staffUserId: package.StaffUserId,
                staffUsername: staffName);

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogPackageDispatched(_logger, package.Id, null);
            return new PackageCommandResult(true, false, true, "Package dispatched.", null);
        }
    }
}
