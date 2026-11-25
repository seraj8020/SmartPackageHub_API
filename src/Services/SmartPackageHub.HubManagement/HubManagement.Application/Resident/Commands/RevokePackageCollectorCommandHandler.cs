using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed class RevokePackageCollectorCommandHandler
        : CreateCommandHandler<RevokePackageCollectorCommand>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogRevoked =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(RevokePackageCollectorCommandHandler)),
                "Revoked authorized collectors for package {PackageId}");

        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<AuthorizedCollector> _collectors;
        private readonly ILogger<RevokePackageCollectorCommandHandler> _logger;

        public RevokePackageCollectorCommandHandler(
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<AuthorizedCollector> collectors,
            ILogger<RevokePackageCollectorCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _residents = residents;
            _packages = packages;
            _collectors = collectors;
            _logger = logger;
        }

        protected override async Task<Guid> HandleAsync(RevokePackageCollectorCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Resident was not found.");

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Package was not found.");

            if (package.ResidentId != resident.Id)
                throw new InvalidOperationException("Package does not belong to this resident.");

            var existing = await _collectors.ListAsync(
                    row => row.PackageId == package.Id && row.IsActive)
                .ConfigureAwait(false);

            if (existing.Count == 0)
                throw new InvalidOperationException("No active authorized collector to revoke.");

            foreach (var row in existing)
            {
                row.Revoke();
                _collectors.Update(row);
            }

            await UnitOfWork!.CommitAsync().ConfigureAwait(false);
            LogRevoked(_logger, package.Id, null);
            return package.Id;
        }
    }
}
