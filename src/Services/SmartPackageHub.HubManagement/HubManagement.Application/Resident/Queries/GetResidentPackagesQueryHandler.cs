using AutoMapper;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentPackagesQueryHandler : QueryHandler<GetResidentPackagesQuery, List<ResidentPackageDto>?>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;

        public GetResidentPackagesQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents,
            IRepository<Package> packages) : base(mapper)
        {
            _residents = residents;
            _packages = packages;
        }

        protected override async Task<List<ResidentPackageDto>?> HandleAsync(GetResidentPackagesQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident == null)
                return null;

            var waiting = _packages
                .FetchMulti(p => p.ResidentId == resident.Id && p.Status != PackageStatus.Delivered)
                .ToList();

            return waiting.Select(p => new ResidentPackageDto
            {
                Id = p.Id,
                TrackingNumber = p.SphTrackingNumber.Value,
                Description = p.Category,
                ReceivedAt = p.RecievedAt,
                Courier = p.CourierPartner,
                Category = p.Category,
                Weight = p.Dimensions.Weight,
                PkgNumber = p.PkgNumber,
                TowerWing = p.TowerWing,
                FlatNumber = p.FlatNumber.Value
            }).ToList();
        }
    }
}
