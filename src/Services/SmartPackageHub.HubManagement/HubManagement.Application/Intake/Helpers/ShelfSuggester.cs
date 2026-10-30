using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;

namespace HubManagement.Application.Intake.Helpers
{
    internal static class ShelfSuggester
    {
        public static Shelf? Suggest(
            IEnumerable<Shelf> shelves,
            IEnumerable<Package> openPackages,
            Guid? societyId,
            bool requiresColdStorage = false)
        {
            var candidates = shelves.Where(shelf => societyId == null || shelf.SocietyId == societyId).ToList();
            var suitable = requiresColdStorage
                ? candidates.Where(shelf => shelf.IsColdStorage).ToList()
                : candidates.Where(shelf => !shelf.IsColdStorage).ToList();
            // A suitable temperature zone is preferred, but a package may still be checked in
            // when only the other type of shelf exists.
            if (suitable.Count > 0)
                candidates = suitable;

            if (candidates.Count == 0)
                return null;

            var counts = openPackages
                .Where(package => package.Status != PackageStatus.Delivered && package.ShelfId != null)
                .GroupBy(package => package.ShelfId!.Value)
                .ToDictionary(group => group.Key, group => group.Count());

            return candidates
                .OrderBy(shelf => shelf.Capacity > 0 && counts.GetValueOrDefault(shelf.Id) >= shelf.Capacity)
                .ThenBy(shelf => counts.GetValueOrDefault(shelf.Id))
                .ThenBy(shelf => shelf.Code, StringComparer.Ordinal)
                .First();
        }
    }
}
