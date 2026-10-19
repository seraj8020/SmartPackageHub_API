using AutoMapper;
using HubManagement.Application.Intake.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Intake.Queries
{
    public sealed record ParseLabelQuery(string? Text) : Query<ParsedLabelDto?>;

    public sealed class ParseLabelQueryHandler : QueryHandler<ParseLabelQuery, ParsedLabelDto?>
    {
        public ParseLabelQueryHandler(IMapper mapper) : base(mapper)
        {
        }

        protected override Task<ParsedLabelDto?> HandleAsync(ParseLabelQuery request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return Task.FromResult<ParsedLabelDto?>(null);

            return Task.FromResult<ParsedLabelDto?>(LabelTextParser.Parse(request.Text));
        }
    }

    public sealed record MatchResidentsQuery(string? Tower, string? Flat, string? Name) : Query<MatchResponse>;

    public sealed class MatchResidentsQueryHandler : QueryHandler<MatchResidentsQuery, MatchResponse>
    {
        private readonly IRepository<Resident> _residents;

        public MatchResidentsQueryHandler(IMapper mapper, IRepository<Resident> residents) : base(mapper)
        {
            _residents = residents;
        }

        protected override Task<MatchResponse> HandleAsync(MatchResidentsQuery request)
        {
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

            return Task.FromResult(new MatchResponse { Status = "missing", Matches = new() });
        }
    }

    public sealed record SearchResidentsQuery(string? Query) : Query<List<ResidentMatchDto>>;

    public sealed class SearchResidentsQueryHandler : QueryHandler<SearchResidentsQuery, List<ResidentMatchDto>>
    {
        private readonly IRepository<Resident> _residents;

        public SearchResidentsQueryHandler(IMapper mapper, IRepository<Resident> residents) : base(mapper)
        {
            _residents = residents;
        }

        protected override Task<List<ResidentMatchDto>> HandleAsync(SearchResidentsQuery request)
        {
            var matches = ResidentMatcher.Search(_residents.GetAll().ToList(), request.Query);
            return Task.FromResult(matches.Select(ResidentMatcher.ToDto).ToList());
        }
    }

    public sealed record SuggestShelfQuery(Guid? SocietyId) : Query<ShelfResponse?>;

    public sealed class SuggestShelfQueryHandler : QueryHandler<SuggestShelfQuery, ShelfResponse?>
    {
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<Package> _packages;

        public SuggestShelfQueryHandler(IMapper mapper, IRepository<Shelf> shelves, IRepository<Package> packages) : base(mapper)
        {
            _shelves = shelves;
            _packages = packages;
        }

        protected override Task<ShelfResponse?> HandleAsync(SuggestShelfQuery request)
        {
            var shelf = ShelfSuggester.Suggest(_shelves.GetAll().ToList(), _packages.GetAll().ToList(), request.SocietyId);
            if (shelf == null)
                return Task.FromResult<ShelfResponse?>(null);

            return Task.FromResult<ShelfResponse?>(new ShelfResponse
            {
                ShelfId = shelf.Id,
                SocietyId = shelf.SocietyId,
                ShelfCode = shelf.Code
            });
        }
    }
}
