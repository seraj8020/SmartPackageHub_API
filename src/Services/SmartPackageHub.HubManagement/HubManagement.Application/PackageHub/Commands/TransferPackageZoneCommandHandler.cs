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
    /// Logs an internal zone transfer (e.g. Hub → Tower B cage) without changing package status.
    /// </summary>
    public sealed class TransferPackageZoneCommandHandler
        : CreateCommandHandler<TransferPackageZoneCommand, PackageCommandResult>
    {
        private static readonly Action<ILogger, Guid, string, string, Exception?> LogTransferred =
            LoggerMessage.Define<Guid, string, string>(
                LogLevel.Information,
                new EventId(1, nameof(TransferPackageZoneCommandHandler)),
                "Package {PackageId} transferred {FromZone} → {ToZone}");

        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageCustodyHop> _hops;
        private readonly IRepository<User> _users;
        private readonly ILogger<TransferPackageZoneCommandHandler> _logger;

        public TransferPackageZoneCommandHandler(
            IRepository<Package> packages,
            IRepository<PackageCustodyHop> hops,
            IRepository<User> users,
            ILogger<TransferPackageZoneCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _hops = hops;
            _users = users;
            _logger = logger;
        }

        public override async Task<PackageCommandResult> Handle(
            TransferPackageZoneCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var payload = request.Request ?? new TransferPackageZoneRequest();
            var toZone = payload.ToZone?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(toZone))
                return new PackageCommandResult(true, false, false, "Destination zone is required.", null);

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            if (package is null)
                return new PackageCommandResult(false, false, false, "Package was not found.", null);
            if (!request.StaffSocietyIds.Contains(package.SocietyId))
                return new PackageCommandResult(true, false, false, "Staff is not assigned to this package's society.", null);

            if (package.Status is not (PackageStatus.Received or PackageStatus.Shelved))
            {
                return new PackageCommandResult(
                    true,
                    true,
                    false,
                    $"Only received or shelved packages can be zone-transferred (current: {package.Status}).",
                    null);
            }

            var fromZone = string.IsNullOrWhiteSpace(payload.FromZone)
                ? PackageCustodyRecorder.LatestZone(_hops, package.Id) ?? PackageCustodyHopKinds.DefaultHubZone
                : payload.FromZone.Trim();

            if (string.Equals(fromZone, toZone, StringComparison.OrdinalIgnoreCase))
                return new PackageCommandResult(true, false, false, "Destination zone must differ from the current zone.", null);

            var staffUsername = await PackageCustodyRecorder
                .ResolveUsernameAsync(_users, request.StaffUserId, cancellationToken)
                .ConfigureAwait(false);

            PackageCustodyRecorder.Insert(
                _hops,
                package.Id,
                package.SocietyId,
                PackageCustodyHopKinds.ZoneTransfer,
                DateTime.UtcNow,
                fromZone: fromZone,
                toZone: toZone,
                locationLabel: payload.LocationLabel,
                staffUserId: request.StaffUserId,
                staffUsername: staffUsername,
                notes: payload.Notes);

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogTransferred(_logger, package.Id, fromZone, toZone, null);

            return new PackageCommandResult(
                true,
                false,
                true,
                $"Package left {fromZone} → arrived {toZone}.",
                PackageDetailsDto.From(package));
        }
    }
}
