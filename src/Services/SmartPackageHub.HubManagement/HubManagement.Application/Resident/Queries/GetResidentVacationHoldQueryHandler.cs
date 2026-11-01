using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentVacationHoldQueryHandler
        : QueryHandler<GetResidentVacationHoldQuery, VacationHoldDto?>
    {
        private readonly IRepository<VacationHold> _holds;
        private readonly IRepository<Resident> _residents;

        public GetResidentVacationHoldQueryHandler(
            AutoMapper.IMapper mapper,
            IRepository<VacationHold> holds,
            IRepository<Resident> residents) : base(mapper)
        {
            _holds = holds;
            _residents = residents;
        }

        protected override async Task<VacationHoldDto?> HandleAsync(GetResidentVacationHoldQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident is null)
                return null;

            var today = DateTime.UtcNow.Date;
            var holds = await _holds.ListAsync(
                hold => hold.ResidentId == request.ResidentId && hold.IsActive,
                noTracking: true).ConfigureAwait(false);

            var active = holds
                .OrderByDescending(hold => hold.ToDate)
                .FirstOrDefault(hold => hold.ToDate >= today);

            if (active is null)
                return null;

            return new VacationHoldDto
            {
                Id = active.Id,
                SocietyId = active.SocietyId,
                ResidentId = active.ResidentId,
                StartDate = active.FromDate,
                EndDate = active.ToDate,
                IsActive = active.IsActive,
                TowerWing = resident.TowerWing,
                FlatNumber = resident.Unit?.Value,
            };
        }
    }
}
