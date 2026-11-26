using AutoMapper;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentProfileQueryHandler
        : QueryHandler<GetResidentProfileQuery, ResidentProfileDto?>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Society> _societies;

        public GetResidentProfileQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents,
            IRepository<Society> societies) : base(mapper)
        {
            _residents = residents;
            _societies = societies;
        }

        protected override async Task<ResidentProfileDto?> HandleAsync(GetResidentProfileQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident is null)
                return null;

            var society = await _societies.GetByIdAsync(resident.SocietyId).ConfigureAwait(false);

            return new ResidentProfileDto
            {
                Id = resident.Id,
                SocietyId = resident.SocietyId,
                SocietyName = society?.Name,
                FullName = resident.FullName,
                PhoneNumber = resident.PhoneNumber,
                TowerWing = resident.TowerWing,
                FlatNumber = resident.Unit?.Value,
            };
        }
    }
}
