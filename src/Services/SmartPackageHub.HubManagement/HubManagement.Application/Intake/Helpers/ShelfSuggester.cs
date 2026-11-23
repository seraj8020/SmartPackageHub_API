using HubManagement.Application.Intake.Converters;
using HubManagement.Application.PackageHub.Services;
using HubManagement.Core.Hubs.Entities;

namespace HubManagement.Application.Intake.Helpers
{
    internal static class ShelfSuggester
    {
        public static Shelf? Suggest(
            IEnumerable<Shelf> shelves,
            IEnumerable<Package> openPackages,
            Guid? societyId,
            bool requiresColdStorage = false,
            string? towerOrWing = null)
        {
            var candidates = shelves.Where(shelf => societyId == null || shelf.SocietyId == societyId).ToList();

            var suitable = requiresColdStorage
                ? candidates.Where(shelf => shelf.IsColdStorage).ToList()
                : candidates.Where(shelf => !shelf.IsColdStorage).ToList();
            // A suitable temperature zone is preferred, but a package may still be checked in
            // when only the other type of shelf exists.
            if (suitable.Count > 0)
                candidates = suitable;

            // Ambient shelves are tower-scoped; cold shelves are shared society-wide.
            if (!requiresColdStorage &&
                candidates.TrueForAll(shelf => !shelf.IsColdStorage) &&
                !string.IsNullOrWhiteSpace(towerOrWing))
            {
                candidates = candidates
                    .Where(shelf => ResidentMatcher.TowerEquals(shelf.TowerOrWing, towerOrWing))
                    .ToList();
            }

            if (candidates.Count == 0)
                return null;

            // Occupancy includes Dispatched so door-out packages keep their A-12-3 reserved.
            var counts = openPackages
                .Where(ShelfOccupancy.OccupiesSlot)
                .GroupBy(package => package.ShelfId!.Value)
                .ToDictionary(group => group.Key, group => group.Count());

            // Prefer free slots (capacity typically 1 for true A-12-3 addresses).
            var free = candidates
                .Where(shelf => shelf.Capacity <= 0 || counts.GetValueOrDefault(shelf.Id) < shelf.Capacity)
                .ToList();
            if (free.Count > 0)
                candidates = free;

            return candidates
                .OrderBy(shelf => counts.GetValueOrDefault(shelf.Id))
                .ThenBy(shelf => shelf.Code, StringComparer.Ordinal)
                .FirstOrDefault();
        }
    }
}
