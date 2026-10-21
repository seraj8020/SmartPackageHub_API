using HubManagement.Application.PackageHub.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed class CreatePackageCommandHandler : CreateCommandHandler<CreatePackageCommand, PackageCommandResult>
    {
        private static readonly Action<ILogger, Guid, string?, Exception?> LogPackageCreated =
            LoggerMessage.Define<Guid, string?>(
                LogLevel.Information,
                new EventId(1, nameof(CreatePackageCommandHandler)),
                "Created package {PackageId} for tracking {TrackingNumber}");
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<User> _users;
        private readonly ILogger<CreatePackageCommandHandler> _logger;

        public CreatePackageCommandHandler(
            IRepository<Package> packages,
            IRepository<Resident> residents,
            IRepository<Society> societies,
            IRepository<User> users,
            ILogger<CreatePackageCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _residents = residents;
            _societies = societies;
            _users = users;
            _logger = logger;
        }

        public override async Task<PackageCommandResult> Handle(CreatePackageCommand command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            var request = command.Request;
            var trackingError = HubPackageFactory.ValidateTracking(request.TrackingNumber, 100);
            if (trackingError != null)
                return new PackageCommandResult(true, false, false, trackingError, null);

            var tracking = request.TrackingNumber!.Trim();
            var open = HubPackageFactory.FindOpen(_packages, tracking);
            if (open != null)
                return new PackageCommandResult(true, true, false, $"This tracking number is already at the hub ({open.PkgNumber}).", null);

            var placed = await PackagePlacement.ResolveAsync(_residents, _societies, _users, request.ResidentId, request.SocietyId, request.StaffUserId, requireResidentWhenSocietyMissing: true).ConfigureAwait(false);
            if (!placed.Succeeded)
                return new PackageCommandResult(placed.Found, false, false, placed.Message, null);

            var package = HubPackageFactory.Create(
                placed.SocietyId,
                placed.Resident,
                null,
                placed.StaffUserId,
                tracking,
                HubPackageFactory.NextNumber(_packages),
                request.Courier,
                request.Description,
                request.RecipientName,
                request.Tower,
                request.FlatNumber,
                false);

            _packages.Insert(package);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogPackageCreated(_logger, package.Id, tracking, null);
            return new PackageCommandResult(true, false, true, "Package created.", PackageDetailsDto.From(package));
        }
    }
}
