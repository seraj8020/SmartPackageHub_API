using HubManagement.Core.Hubs.Entities;
using HubManagement.Application.DeliverySlots.Helpers;

namespace HubManagement.Application.Societies.DTOs
{
    internal static class SocietyPackageMapper
    {
        public static SocietyPackageDto Map(
            Package package,
            IReadOnlyDictionary<Guid, string> shelfCodesById,
            DateTime? deliveredAt = null,
            DeliverySlot? slot = null,
            IReadOnlyDictionary<Guid, int>? shelfCapacitiesById = null,
            IReadOnlyDictionary<Guid, bool>? shelfColdById = null)
        {
            ArgumentNullException.ThrowIfNull(package);
            string? shelfCode = null;
            var shelfIsCold = false;
            if (package.ShelfId is Guid shelfId)
            {
                if (shelfCodesById.TryGetValue(shelfId, out var code))
                    shelfCode = code;
                if (shelfColdById != null && shelfColdById.TryGetValue(shelfId, out var cold))
                    shelfIsCold = cold;
            }

            // Capacity comes from the assigned shelf when available.
            int? shelfCapacity = null;
            if (package.ShelfId is Guid capacityShelfId &&
                shelfCapacitiesById != null &&
                shelfCapacitiesById.TryGetValue(capacityShelfId, out var capacity))
            {
                shelfCapacity = capacity;
            }

            return new SocietyPackageDto
            {
                Id = package.Id,
                ResidentId = package.ResidentId,
                StaffUserId = package.StaffUserId,
                TrackingNumber = package.SphTrackingNumber.Value,
                RecipientName = package.RecipientName,
                ReceivedAt = package.RecievedAt,
                DeliveredAt = deliveredAt,
                Courier = package.CourierPartner,
                Category = package.Category,
                Weight = package.Dimensions.Weight,
                TowerWing = string.IsNullOrWhiteSpace(package.TowerWing) ? null : package.TowerWing,
                FlatNumber = package.FlatNumber.Value,
                ShelfCode = shelfCode,
                ShelfCapacity = shelfCapacity,
                IsColdStorage = package.IsColdStorage || shelfIsCold,
                Status = package.Status.ToString(),
                IsCod = package.IsCod,
                CodAmount = package.CodAmount,
                CodApprovalStatus = package.CodApprovalStatus.ToString(),
                DeliverySlotStatus = slot?.Status,
                DeliverySlotLabel = slot == null
                    ? null
                    : DeliverySlotWindows.LabelFor(slot.WindowStart, slot.WindowEnd),
            };
        }

        public static Dictionary<Guid, string> BuildShelfCodeLookup(
            IEnumerable<Package> packages,
            IEnumerable<Shelf> shelves)
        {
            var needed = packages
                .Where(package => package.ShelfId.HasValue)
                .Select(package => package.ShelfId!.Value)
                .ToHashSet();

            if (needed.Count == 0)
                return new Dictionary<Guid, string>();

            return shelves
                .Where(shelf => needed.Contains(shelf.Id))
                .ToDictionary(shelf => shelf.Id, shelf => shelf.Code);
        }

        public static Dictionary<Guid, DeliverySlot> BuildActiveSlotLookup(
            IEnumerable<Package> packages,
            IEnumerable<DeliverySlot> slots)
        {
            var packageIds = packages.Select(package => package.Id).ToHashSet();
            return slots
                .Where(slot =>
                    packageIds.Contains(slot.PackageId) &&
                    !string.Equals(slot.Status, DeliverySlotWindows.StatusCancelled, StringComparison.Ordinal))
                .GroupBy(slot => slot.PackageId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(slot => slot.WindowStart).First());
        }
    }
}
