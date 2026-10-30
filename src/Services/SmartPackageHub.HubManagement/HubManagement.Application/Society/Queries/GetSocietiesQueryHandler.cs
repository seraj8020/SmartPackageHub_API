using AutoMapper;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Queries
{
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
                .AsEnumerable() // Switches to in-memory processing for the DTO projection
                .Select(SocietyDetailsDto.From)
                .ToList();

            return Task.FromResult(societies);
        }
    }
}
