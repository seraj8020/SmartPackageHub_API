using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;

namespace HubManagement.Application.PackageHub.Services
{
    /// <summary>
    /// Slot occupancy rules for first-class shelf codes (e.g. A-12-3).
    /// Dispatched packages keep the slot reserved until real exit (delivered / return closed).
    /// </summary>
    public static class ShelfOccupancy
    {
        public static bool OccupiesSlot(PackageStatus status) =>
            status is PackageStatus.Shelved
                or PackageStatus.Dispatched
                or PackageStatus.ReturnShelved;

        public static bool OccupiesSlot(Package package)
        {
            ArgumentNullException.ThrowIfNull(package);
            return package.ShelfId.HasValue && OccupiesSlot(package.Status);
        }

        public static int CountOnShelf(IEnumerable<Package> packages, Guid shelfId)
        {
            ArgumentNullException.ThrowIfNull(packages);
            return packages.Count(package => package.ShelfId == shelfId && OccupiesSlot(package));
        }

        public static bool IsFull(Shelf shelf, IEnumerable<Package> packages)
        {
            ArgumentNullException.ThrowIfNull(shelf);
            ArgumentNullException.ThrowIfNull(packages);
            if (shelf.Capacity <= 0)
                return false;
            return CountOnShelf(packages, shelf.Id) >= shelf.Capacity;
        }

        /// <summary>Builds slot code Tower-Rack-Tier → A-12-3.</summary>
        public static string BuildSlotCode(string towerOrZone, string rackOrAisle, int tier)
        {
            ArgumentNullException.ThrowIfNull(towerOrZone);
            ArgumentNullException.ThrowIfNull(rackOrAisle);
            var zone = towerOrZone.Trim().ToUpperInvariant();
            var aisle = rackOrAisle.Trim();
            if (zone.StartsWith("TOWER ", StringComparison.OrdinalIgnoreCase))
                zone = zone["TOWER ".Length..].Trim();
            return $"{zone}-{aisle}-{tier}";
        }
    }
}
