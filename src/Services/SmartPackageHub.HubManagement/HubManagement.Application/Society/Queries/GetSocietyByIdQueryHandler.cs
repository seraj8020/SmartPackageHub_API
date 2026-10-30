using AutoMapper;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Queries
{
    public sealed class GetSocietyByIdQueryHandler : QueryHandler<GetSocietyByIdQuery, List<SocietyDto>>
    {
        private readonly IRepository<Society> _repository;

        public GetSocietyByIdQueryHandler(IMapper mapper,
            IRepository<Society> repository) : base(mapper)
        {
            _repository = repository;
        }

        protected override async Task<List<SocietyDto>> HandleAsync(GetSocietyByIdQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var accounts = await _repository.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            return Mapper.Map<List<SocietyDto>>(accounts);
        }
    }
}
