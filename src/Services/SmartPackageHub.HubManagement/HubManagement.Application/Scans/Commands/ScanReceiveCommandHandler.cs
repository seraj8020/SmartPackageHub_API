using HubManagement.Application.PackageHub.DTOs;
using HubManagement.Application.PackageHub.Services;
using HubManagement.Application.Scans.Helpers;
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
        private readonly IRepository<UserSociety> _userSocieties;
        private readonly ILogger<ScanReceiveCommandHandler> _logger;

        public ScanReceiveCommandHandler(
            IRepository<Package> packages,
            IRepository<Resident> residents,
            IRepository<Society> societies,
            IRepository<User> users,
            IRepository<UserSociety> userSocieties,
            ILogger<ScanReceiveCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _residents = residents;
            _societies = societies;
            _users = users;
            _userSocieties = userSocieties;
            _logger = logger;
        }

        public override async Task<PackageCommandResult> Handle(ScanReceiveCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var scan = request.Request;
            if (string.IsNullOrWhiteSpace(scan.Vendor) || string.IsNullOrWhiteSpace(scan.RawCode))
                return new PackageCommandResult(true, false, false, "Vendor and RawCode are required", null);

            var tracking = ScanCodeParser.ParseTracking(scan.Vendor, scan.RawCode) ?? scan.RawCode.Trim();
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
                _userSocieties,
                scan.ResidentId,
                scan.SocietyId,
                scan.StaffUserId,
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
                scan.Vendor,
                scan.Description,
                scan.RecipientName,
                scan.Tower,
                scan.FlatNumber,
                false);

            _packages.Insert(package);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogScanReceived(_logger, package.Id, scan.Vendor, null);
            return new PackageCommandResult(true, false, true, "Package received", PackageDetailsDto.From(package));
        }
    }

    
}
