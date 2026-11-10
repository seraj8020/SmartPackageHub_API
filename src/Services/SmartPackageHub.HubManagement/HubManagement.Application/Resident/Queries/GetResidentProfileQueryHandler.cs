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

        public GetResidentProfileQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents) : base(mapper)
        {
            _residents = residents;
        }

        protected override async Task<ResidentProfileDto?> HandleAsync(GetResidentProfileQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident is null)
                return null;

            return new ResidentProfileDto
            {
                Id = resident.Id,
                SocietyId = resident.SocietyId,
                FullName = resident.FullName,
                PhoneNumber = resident.PhoneNumber,
                TowerWing = resident.TowerWing,
                FlatNumber = resident.Unit?.Value,
            };
        }
    }
}
