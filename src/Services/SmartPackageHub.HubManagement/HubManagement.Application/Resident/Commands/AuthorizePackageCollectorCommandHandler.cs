using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed class AuthorizePackageCollectorCommandHandler
        : CreateCommandHandler<AuthorizePackageCollectorCommand>
    {
        private static readonly Action<ILogger, Guid, Guid, Exception?> LogAuthorized =
            LoggerMessage.Define<Guid, Guid>(
                LogLevel.Information,
                new EventId(1, nameof(AuthorizePackageCollectorCommandHandler)),
                "Authorized collector {CollectorId} for package {PackageId}");

        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<AuthorizedCollector> _collectors;
        private readonly ILogger<AuthorizePackageCollectorCommandHandler> _logger;

        public AuthorizePackageCollectorCommandHandler(
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<AuthorizedCollector> collectors,
            ILogger<AuthorizePackageCollectorCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _residents = residents;
            _packages = packages;
            _collectors = collectors;
            _logger = logger;
        }

        protected override async Task<Guid> HandleAsync(AuthorizePackageCollectorCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Resident was not found.");

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Package was not found.");

            if (package.ResidentId != resident.Id)
                throw new InvalidOperationException("Package does not belong to this resident.");

            if (package.Status is PackageStatus.Delivered or PackageStatus.ReturnRequested
                or PackageStatus.ReturnReceived or PackageStatus.ReturnShelved
                or PackageStatus.ReturnPickedByCourier or PackageStatus.ReturnClosed)
            {
                throw new InvalidOperationException("Only packages still at the hub can have an authorized collector.");
            }

            var existing = await _collectors.ListAsync(
                    row => row.PackageId == package.Id && row.IsActive)
                .ConfigureAwait(false);
            foreach (var row in existing)
            {
                row.Revoke();
                _collectors.Update(row);
            }

            var expires = request.ExpiresAtUtc ?? DateTime.UtcNow.AddDays(7);
            var created = AuthorizedCollector.Create(
                package.Id,
                package.SocietyId,
                resident.Id,
                request.CollectorName,
                request.CollectorPhone,
                request.Note,
                expires);

            _collectors.Insert(created);
            await UnitOfWork!.CommitAsync().ConfigureAwait(false);
            LogAuthorized(_logger, created.Id, package.Id, null);
            return created.Id;
        }
    }
}
