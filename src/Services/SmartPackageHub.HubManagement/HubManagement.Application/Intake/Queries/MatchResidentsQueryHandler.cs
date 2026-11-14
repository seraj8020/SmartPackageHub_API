using AutoMapper;
using HubManagement.Application.Intake.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;
using HubManagement.Application.Intake.Converters;
using HubManagement.Application.Intake.Helpers;

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

            IEnumerable<Resident> residents = _residents.GetAll();
            if (request.SocietyId is Guid societyId && societyId != Guid.Empty)
                residents = residents.Where(resident => resident.SocietyId == societyId);

            var societyResidents = residents.ToList();

            var byUnit = MatchByUnit(societyResidents, tower, flat);
            if (byUnit != null)
                return Task.FromResult(byUnit);

            var byName = MatchByName(societyResidents, request.Name);
            if (byName != null)
                return Task.FromResult(byName);

            return Task.FromResult(new MatchResponse { Status = "missing", Matches = new List<ResidentMatchDto>() });
        }

        private static MatchResponse? MatchByUnit(List<Resident> societyResidents, string? tower, string? flat)
        {
            if (tower != null && flat != null)
            {
                var hits = ResidentMatcher.ByTowerAndFlat(societyResidents, tower, flat).Take(8).ToList();
                return ToUnitResponse(hits);
            }

            // Many Indian labels print only "Flat 905" with no tower/wing.
            if (tower == null && flat != null)
            {
                var byFlat = societyResidents
                    .Where(resident => ResidentMatcher.FlatEquals(resident.Unit?.Value, flat))
                    .OrderByDescending(resident => resident.IsActive)
                    .ThenBy(resident => resident.FullName, StringComparer.OrdinalIgnoreCase)
                    .Take(8)
                    .ToList();
                return ToUnitResponse(byFlat);
            }

            return null;
        }

        private static MatchResponse? MatchByName(List<Resident> societyResidents, string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var trimmed = name.Trim();
            var byName = ResidentMatcher.Search(societyResidents, trimmed);
            var named = ToUnitResponse(byName);
            if (named != null)
                return named;

            // OCR often returns a partial name ("fozia"); try each token ≥4 chars.
            foreach (var token in trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (token.Length < 4)
                    continue;
                var tokenHits = ResidentMatcher.Search(societyResidents, token);
                var tokenResponse = ToUnitResponse(tokenHits);
                if (tokenResponse != null)
                    return tokenResponse;
            }

            // OCR glue/mangle: FehenKh ≈ Rehan Khan (no tower/flat on this label read).
            return ToUnitResponse(ResidentMatcher.FuzzyByName(societyResidents, trimmed));
        }

        private static MatchResponse? ToUnitResponse(List<Resident> hits)
        {
            if (hits.Count == 1)
                return new MatchResponse { Status = "matched", Matches = hits.Select(ResidentMatcher.ToDto).ToList() };
            if (hits.Count > 1)
                return new MatchResponse { Status = "ambiguous", Matches = hits.Select(ResidentMatcher.ToDto).ToList() };
            return null;
        }
    }
}
