using AutoMapper;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Queries
{
    public sealed class DispatchedPackagesQueryHandler : QueryHandler<DispatchedPackagesQuery, List<SocietyPackageDto>?>
    {
        private readonly IRepository<Society> _societies;
        private readonly IRepository<Package> _packages;
        public DispatchedPackagesQueryHandler(IMapper mapper, IRepository<Society> societies, IRepository<Package> packages) : base(mapper)
        {
            _societies = societies;
            _packages = packages;
        }

        protected override async Task<List<SocietyPackageDto>?> HandleAsync(DispatchedPackagesQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var society = await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            if (society == null)
                return null;
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var waiting = _packages
                .FetchMulti(p => p.SocietyId == society.Id && p.Status == PackageStatus.Dispatched)
                .ToList();

            return waiting.Select(p => new SocietyPackageDto
            {
                Id = p.Id,
                ResidentId = p.ResidentId,
                StaffUserId = p.StaffUserId,
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
