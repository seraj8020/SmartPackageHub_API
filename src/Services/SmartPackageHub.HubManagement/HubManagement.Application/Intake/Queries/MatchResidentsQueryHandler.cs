using AutoMapper;
using HubManagement.Application.Intake.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Intake.Queries
{
    public sealed class MatchResidentsQueryHandler : QueryHandler<MatchResidentsQuery, MatchResponse>
    {
        private readonly IRepository<Resident> _residents;

        public MatchResidentsQueryHandler(IMapper mapper, IRepository<Resident> residents) : base(mapper)
        {
            _residents = residents;
        }

        protected override Task<MatchResponse> HandleAsync(MatchResidentsQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var tower = request.Tower;
            var flat = request.Flat;
            IntakeText.SplitUnit(ref tower, ref flat);
            tower = LabelTextParser.CleanTower(tower);
            flat = LabelTextParser.CleanFlat(flat);

            var residents = _residents.GetAll().ToList();
            if (tower != null && flat != null)
            {
                var hits = ResidentMatcher.ByTowerAndFlat(residents, tower, flat).Take(8).ToList();
                if (hits.Count == 1)
                    return Task.FromResult(new MatchResponse { Status = "matched", Matches = hits.Select(ResidentMatcher.ToDto).ToList() });
                if (hits.Count > 1)
                    return Task.FromResult(new MatchResponse { Status = "ambiguous", Matches = hits.Select(ResidentMatcher.ToDto).ToList() });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var byName = ResidentMatcher.Search(residents, request.Name.Trim());
                if (byName.Count > 0)
                    return Task.FromResult(new MatchResponse { Status = "ambiguous", Matches = byName.Select(ResidentMatcher.ToDto).ToList() });
            }

            return Task.FromResult(new MatchResponse { Status = "missing", Matches =new List<ResidentMatchDto>() });
        }
    }
}
