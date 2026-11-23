using System.Globalization;
using AutoMapper;
using HubManagement.Application.Intake.Converters;
using HubManagement.Application.Intake.DTOs;
using HubManagement.Application.Intake.Helpers;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Intake.Queries
{
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
            ArgumentNullException.ThrowIfNull(request);
            var societyId = request.SocietyId;
            var societyShelves = (societyId == null
                ? _shelves.GetAll(noTracking: true)
                : _shelves.FetchMulti(shelf => shelf.SocietyId == societyId, noTracking: true)).ToList();

            var wantsCold = request.RequiresColdStorage == true;
            // Ambient: scope to resident tower. Cold: society-shared (not tower-filtered).
            var scoped = wantsCold || string.IsNullOrWhiteSpace(request.TowerOrWing)
                ? societyShelves
                : societyShelves
                    .Where(shelf =>
                        !shelf.IsColdStorage &&
                        ResidentMatcher.TowerEquals(shelf.TowerOrWing, request.TowerOrWing))
                    .ToList();

            // Prefer true slots (G-2-3) over legacy A-1 codes when both exist for the society.
            var typed = scoped.Where(shelf => shelf.IsColdStorage == wantsCold).ToList();
            var trueSlots = typed.Where(shelf => IsTrueSlotCode(shelf.Code)).ToList();
            var legacySlots = typed.Where(shelf => IsLegacyHubUiShelfCode(shelf.Code)).ToList();
            List<Shelf> preferred;
            if (trueSlots.Count > 0)
                preferred = trueSlots;
            else if (legacySlots.Count > 0)
                preferred = legacySlots;
            else
                preferred = typed;
            var candidates = preferred.Count > 0 ? preferred : scoped;
            var packages = (societyId == null
                ? _packages.GetAll(noTracking: true)
                : _packages.FetchMulti(package => package.SocietyId == societyId, noTracking: true)).ToList();

            var shelf = ShelfSuggester.Suggest(
                candidates,
                packages,
                societyId,
                wantsCold,
                request.TowerOrWing);
            if (shelf == null)
                return Task.FromResult<ShelfResponse?>(null);

            return Task.FromResult<ShelfResponse?>(new ShelfResponse
            {
                ShelfId = shelf.Id,
                SocietyId = shelf.SocietyId,
                ShelfCode = shelf.Code
            });
        }

        /// <summary>True slot address Tower/Zone-Rack-Tier, e.g. G-2-3 or A-12-1.</summary>
        private static bool IsTrueSlotCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;
            var parts = code.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length == 3
                && parts[0].Length > 0
                && parts[1].Length > 0
                && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var tier)
                && tier > 0;
        }

        /// <summary>Legacy Traizr-style codes A-1 … D-n (towers A–D only).</summary>
        private static bool IsLegacyHubUiShelfCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;
            var parts = code.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length == 2
                && parts[0].Length == 1
                && parts[0][0] is >= 'A' and <= 'D'
                && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var slot)
                && slot > 0;
        }
    }
}
