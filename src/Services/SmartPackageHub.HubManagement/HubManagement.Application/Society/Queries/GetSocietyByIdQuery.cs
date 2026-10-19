using AutoMapper;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Queries
{
    public sealed record GetSocietyByIdQuery(Guid SocietyId) : Query<List<SocietyDTO>>;
    public sealed class GetSocietyByIdQueryHandler : QueryHandler<GetSocietyByIdQuery, List<SocietyDTO>>
    {
        private readonly IRepository<Society> _repository;

        public GetSocietyByIdQueryHandler(IMapper mapper,
            IRepository<Society> repository) : base(mapper)
        {
            _repository = repository;
        }

        protected override async Task<List<SocietyDTO>> HandleAsync(GetSocietyByIdQuery request)
        {
            var accounts = await _repository.GetByIdAsync(request.SocietyId);
            return Mapper.Map<List<SocietyDTO>>(accounts);
        }
    }
}
