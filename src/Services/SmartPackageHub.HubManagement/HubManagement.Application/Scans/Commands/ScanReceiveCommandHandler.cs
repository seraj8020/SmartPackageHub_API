using HubManagement.Application.Intake.Commands;
using HubManagement.Application.PackageHub;
using HubManagement.Application.PackageHub.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Scans.Commands
{
    public sealed class ScanReceiveCommandHandler : CreateCommandHandler<ScanReceiveCommand, PackageCommandResult>
    {
        private static readonly Action<ILogger, Guid, string?, Exception?> LogScanReceived =
            LoggerMessage.Define<Guid, string?>(
                LogLevel.Information,
                new EventId(1, nameof(ScanReceiveCommandHandler)),
                "Scan received package {PackageId} from {Vendor}");
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<User> _users;
        private readonly ILogger<ScanReceiveCommandHandler> _logger;

        public ScanReceiveCommandHandler(
            IRepository<Package> packages,
            IRepository<Resident> residents,
            IRepository<Society> societies,
            IRepository<User> users,
            ILogger<ScanReceiveCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _residents = residents;
            _societies = societies;
            _users = users;
            _logger = logger;
        }

        public override async Task<PackageCommandResult> Handle(ScanReceiveCommand command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            var request = command.Request;
            if (string.IsNullOrWhiteSpace(request.Vendor) || string.IsNullOrWhiteSpace(request.RawCode))
                return new PackageCommandResult(true, false, false, "Vendor and RawCode are required", null);

            var tracking = ScanCodeParser.ParseTracking(request.Vendor, request.RawCode) ?? request.RawCode.Trim();
            var trackingError = HubPackageFactory.ValidateTracking(tracking, 100);
            if (trackingError != null)
                return new PackageCommandResult(true, false, false, trackingError, null);

            tracking = tracking.Trim();
            var open = HubPackageFactory.FindOpen(_packages, tracking);
            if (open != null)
                return new PackageCommandResult(true, true, false, $"This tracking number is already at the hub ({open.PkgNumber}).", null);

            var placed = await PackagePlacement.ResolveAsync(
                _residents,
                _societies,
                _users,
                request.ResidentId,
                request.SocietyId,
                request.StaffUserId,
                requireResidentWhenSocietyMissing: true).ConfigureAwait(false);
            if (!placed.Succeeded)
                return new PackageCommandResult(placed.Found, false, false, placed.Message, null);

            var package = HubPackageFactory.Create(
                placed.SocietyId,
                placed.Resident,
                null,
                placed.StaffUserId,
                tracking,
                HubPackageFactory.NextNumber(_packages),
                request.Vendor,
                request.Description,
                request.RecipientName,
                request.Tower,
                request.FlatNumber,
                false);

            _packages.Insert(package);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogScanReceived(_logger, package.Id, request.Vendor, null);
            return new PackageCommandResult(true, false, true, "Package received", PackageDetailsDto.From(package));
        }
    }

    
}
