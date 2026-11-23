using HubManagement.Application.PackageHub.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed class DispatchPackageCommandHandler : CreateCommandHandler<DispatchPackageCommand, bool>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogPackageDispatched =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(DispatchPackageCommandHandler)),
                "Dispatched package {PackageId}");
        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageCustodyHop> _custodyHops;
        private readonly IRepository<User> _users;
        private readonly ILogger<DispatchPackageCommandHandler> _logger;

        public DispatchPackageCommandHandler(
            IRepository<Package> packages,
            IRepository<PackageCustodyHop> custodyHops,
            IRepository<User> users,
            ILogger<DispatchPackageCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _custodyHops = custodyHops;
            _users = users;
            _logger = logger;
        }

        public override async Task<bool> Handle(DispatchPackageCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var packageTask = _packages.GetByIdAsync(request.packageId);
            // 2. Await the task later using ConfigureAwait(false) to get the result
            var package = await packageTask.ConfigureAwait(false);
            if (package == null)
                return false;

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
            return true;
        }
    }
}
