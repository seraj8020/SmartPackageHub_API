using AutoMapper;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Application.HubResidents.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetPackageAuthorizedCollectorQueryHandler
        : QueryHandler<GetPackageAuthorizedCollectorQuery, AuthorizedCollectorDto?>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<AuthorizedCollector> _collectors;

        public GetPackageAuthorizedCollectorQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<AuthorizedCollector> collectors) : base(mapper)
        {
            _residents = residents;
            _packages = packages;
            _collectors = collectors;
        }

        protected override async Task<AuthorizedCollectorDto?> HandleAsync(GetPackageAuthorizedCollectorQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident is null)
                return null;

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            if (package is null || package.ResidentId != resident.Id)
                return null;

            var collector = await PackageHoldLookup.FindActiveCollectorAsync(
                    _collectors,
                    package.Id,
                    DateTime.UtcNow)
                .ConfigureAwait(false);

            if (collector is null)
                return null;

            return MapCollector(collector);
        }

        internal static AuthorizedCollectorDto MapCollector(AuthorizedCollector collector) =>
            new()
            {
                Id = collector.Id,
                PackageId = collector.PackageId,
                CollectorName = collector.CollectorName,
                CollectorPhone = collector.CollectorPhone,
                Note = collector.Note,
                AuthorizedAtUtc = collector.AuthorizedAtUtc,
                ExpiresAtUtc = collector.ExpiresAtUtc,
                IsActive = collector.IsActive,
            };
    }
}
