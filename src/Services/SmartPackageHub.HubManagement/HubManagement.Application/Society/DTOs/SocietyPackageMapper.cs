using HubManagement.Application.DeliverySlots;
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
            DeliverySlot? slot = null)
        {
            ArgumentNullException.ThrowIfNull(package);
            string? shelfCode = null;
            if (package.ShelfId is Guid shelfId &&
                shelfCodesById.TryGetValue(shelfId, out var code))
            {
                shelfCode = code;
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
                    slot.Status != DeliverySlotWindows.StatusCancelled)
                .GroupBy(slot => slot.PackageId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(slot => slot.WindowStart).First());
        }
    }
}
