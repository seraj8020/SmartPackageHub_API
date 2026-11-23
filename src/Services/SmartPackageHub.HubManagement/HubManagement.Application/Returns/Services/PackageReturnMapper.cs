using HubManagement.Application.Returns.DTOs;
using HubManagement.Core.Hubs.Entities;

namespace HubManagement.Application.Returns.Services
{
    internal static class PackageReturnMapper
    {
        public static PackageReturnDto ToDto(
            PackageReturn entity,
            IEnumerable<PackageReturnImage>? photos = null,
            Package? package = null,
            Resident? resident = null,
            string? shelfCode = null)
        {
            return new PackageReturnDto
            {
                Id = entity.Id,
                PackageId = entity.PackageId,
                SocietyId = entity.SocietyId,
                ResidentId = entity.ResidentId,
                Status = entity.Status.ToString(),
                Reason = entity.Reason.ToString(),
                Note = entity.Note,
                RequestedAtUtc = entity.RequestedAtUtc,
                ReceivedAtUtc = entity.ReceivedAtUtc,
                ReceivedByStaffUserId = entity.ReceivedByStaffUserId,
                ShelfId = entity.ShelfId,
                ShelfCode = shelfCode,
                ShelfAssignedAtUtc = entity.ShelfAssignedAtUtc,
                CourierPickupNote = entity.CourierPickupNote,
                CourierPickupAwb = entity.CourierPickupAwb,
                ClosedAtUtc = entity.ClosedAtUtc,
                PkgNumber = package?.PkgNumber,
                CourierPartner = package?.CourierPartner,
                CourierAwb = package?.CourierAwb,
                ResidentName = resident?.FullName,
                Unit = FormatUnit(resident),
                Photos = (photos ?? entity.Images)
                    .OrderBy(p => p.SortOrder)
                    .Select(p => new PackageReturnImageDto
                    {
                        Id = p.Id,
                        StorageKey = p.StorageKey,
                        SortOrder = p.SortOrder,
                        CapturedAtUtc = p.CapturedAtUtc,
                    })
                    .ToList(),
            };
        }

        private static string? FormatUnit(Resident? resident)
        {
            if (resident is null)
                return null;

            var flat = resident.Unit?.Value;
            var tower = resident.TowerWing;
            if (string.IsNullOrWhiteSpace(tower))
                return flat;
            if (string.IsNullOrWhiteSpace(flat))
                return tower;
            return $"{tower}-{flat}";
        }
    }
}
