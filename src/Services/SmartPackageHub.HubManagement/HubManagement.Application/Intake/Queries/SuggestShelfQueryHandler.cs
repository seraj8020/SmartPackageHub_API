using System.Globalization;
using AutoMapper;
using HubManagement.Application.Intake.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;
using HubManagement.Application.Intake.Helpers;

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
            // Prefer hub UI-style codes (A-1) over legacy codes (PFP-A-01) when both exist.
            var societyShelves = (societyId == null
                ? _shelves.GetAll(noTracking: true)
                : _shelves.FetchMulti(shelf => shelf.SocietyId == societyId, noTracking: true)).ToList();
            var preferred = societyShelves
                .Where(shelf => !shelf.IsColdStorage && IsHubUiShelfCode(shelf.Code))
                .ToList();
            var candidates = preferred.Count > 0 ? preferred : societyShelves;
            var packages = (societyId == null
                ? _packages.GetAll(noTracking: true)
                : _packages.FetchMulti(package => package.SocietyId == societyId, noTracking: true)).ToList();

            var shelf = ShelfSuggester.Suggest(candidates, packages, societyId);
            if (shelf == null)
                return Task.FromResult<ShelfResponse?>(null);

            return Task.FromResult<ShelfResponse?>(new ShelfResponse
            {
                ShelfId = shelf.Id,
                SocietyId = shelf.SocietyId,
                ShelfCode = shelf.Code
            });
        }

        private static bool IsHubUiShelfCode(string code)
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
