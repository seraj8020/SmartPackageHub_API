using AutoMapper;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentByIdQueryHandler : QueryHandler<GetResidentByIdQuery, List<ResidentDto>>
    {
        private readonly IRepository<Resident> _repository;

        public GetResidentByIdQueryHandler(IMapper mapper,
            IRepository<Resident> repository) : base(mapper)
        {
            _repository = repository;
        }

        protected override async Task<List<ResidentDto>> HandleAsync(GetResidentByIdQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var accounts = await _repository.GetByIdAsync(request.residentId).ConfigureAwait(false);
            return Mapper.Map<List<ResidentDto>>(accounts);
        }
    }
}
