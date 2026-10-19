using AutoMapper;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed record GetResidentByIdQuery(Guid residentId) : Query<List<ResidentDTO>>;
    public sealed class GetResidentByIdQueryHandler : QueryHandler<GetResidentByIdQuery, List<ResidentDTO>>
    {
        private readonly IRepository<Resident> _repository;

        public GetResidentByIdQueryHandler(IMapper mapper,
            IRepository<Resident> repository) : base(mapper)
        {
            _repository = repository;
        }

        protected override async Task<List<ResidentDTO>> HandleAsync(GetResidentByIdQuery request)
        {
            var accounts = await _repository.GetByIdAsync(request.residentId);
            return Mapper.Map<List<ResidentDTO>>(accounts);
        }
    }
}
