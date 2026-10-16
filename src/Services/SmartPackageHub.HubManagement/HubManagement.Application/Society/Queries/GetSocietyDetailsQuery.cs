using AutoMapper;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Queries
{
    public sealed record GetSocietiesQuery : Query<List<SocietyDetailsDto>>;

    public sealed class GetSocietiesQueryHandler : QueryHandler<GetSocietiesQuery, List<SocietyDetailsDto>>
    {
        private readonly IRepository<Society> _societies;

        public GetSocietiesQueryHandler(IMapper mapper, IRepository<Society> societies) : base(mapper)
        {
            _societies = societies;
        }

        protected override Task<List<SocietyDetailsDto>> HandleAsync(GetSocietiesQuery request)
        {
            var societies = _societies.GetAll()
                .OrderBy(society => society.Name)
                .ToList()
                .Select(SocietyDetailsDto.From)
                .ToList();

            return Task.FromResult(societies);
        }
    }

    public sealed record GetSocietyDetailsQuery(Guid SocietyId) : Query<SocietyDetailsDto?>;

    public sealed class GetSocietyDetailsQueryHandler : QueryHandler<GetSocietyDetailsQuery, SocietyDetailsDto?>
    {
        private readonly IRepository<Society> _societies;

        public GetSocietyDetailsQueryHandler(IMapper mapper, IRepository<Society> societies) : base(mapper)
        {
            _societies = societies;
        }

        protected override async Task<SocietyDetailsDto?> HandleAsync(GetSocietyDetailsQuery request)
        {
            var society = await _societies.GetByIdAsync(request.SocietyId);
            return society == null ? null : SocietyDetailsDto.From(society);
        }
    }
}
