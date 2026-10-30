using AutoMapper;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Queries
{
    public sealed class GetSocietyDetailsQueryHandler : QueryHandler<GetSocietyBySocietyIdQuery, SocietyDetailsDto?>
    {
        private readonly IRepository<Society> _societies;

        public GetSocietyDetailsQueryHandler(IMapper mapper, IRepository<Society> societies) : base(mapper)
        {
            _societies = societies;
        }

        protected override async Task<SocietyDetailsDto?> HandleAsync(GetSocietyBySocietyIdQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var society = await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            return society == null ? null : SocietyDetailsDto.From(society);
        }
    }
}
