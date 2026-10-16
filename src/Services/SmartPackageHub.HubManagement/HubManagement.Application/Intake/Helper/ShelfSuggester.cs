using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;

namespace HubManagement.Application.Intake
{
    internal static class ShelfSuggester
    {
        public static Shelf? Suggest(IEnumerable<Shelf> shelves, IEnumerable<Package> openPackages, Guid? societyId)
        {
            var candidates = shelves.Where(shelf => societyId == null || shelf.SocietyId == societyId).ToList();
            var ambient = candidates.Where(shelf => !shelf.IsColdStorage).ToList();
            if (ambient.Count > 0)
                candidates = ambient;

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
