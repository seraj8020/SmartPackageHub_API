using AutoMapper;
using HubManagement.Application.Intake.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Intake.Queries
{
    public sealed class SearchResidentsQueryHandler : QueryHandler<SearchResidentsQuery, List<ResidentMatchDto>>
    {
        private readonly IRepository<Resident> _residents;

        public SearchResidentsQueryHandler(IMapper mapper, IRepository<Resident> residents) : base(mapper)
        {
            _residents = residents;
        }

        protected override Task<List<ResidentMatchDto>> HandleAsync(SearchResidentsQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var matches = ResidentMatcher.Search(_residents.GetAll().ToList(), request.Query);
            return Task.FromResult(matches.Select(ResidentMatcher.ToDto).ToList());
        }
    }
}
