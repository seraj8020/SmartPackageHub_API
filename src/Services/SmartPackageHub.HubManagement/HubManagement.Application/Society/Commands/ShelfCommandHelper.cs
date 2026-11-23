using HubManagement.Application.PackageHub.Services;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Domains.Abstractions.Guards;

namespace HubManagement.Application.Societies.Commands
{
    internal static class ShelfCommandHelper
    {
        internal static SocietyShelfDto ToDto(Shelf shelf) => new()
        {
            Id = shelf.Id,
            Code = shelf.Code,
            TowerOrWing = shelf.TowerOrWing,
            IsColdStorage = shelf.IsColdStorage,
            Capacity = shelf.Capacity,
            Zone = shelf.Location?.Zone ?? string.Empty,
            Aisle = shelf.Location?.Aisle ?? string.Empty,
            Tier = shelf.Location?.Tier ?? 0,
        };

        internal static (bool Succeeded, string Message, ShelfLocation? Location) TryCreateLocation(
            string zone,
            string aisle,
            int tier)
        {
            try
            {
                return (true, string.Empty, ShelfLocation.Create(zone.Trim(), aisle.Trim(), tier));
            }
            catch (BusinessRuleException ex)
            {
                return (false, ex.Message, null);
            }
            catch (ArgumentException ex)
            {
                return (false, ex.Message, null);
            }
        }

        internal static string? ValidateFields(string code, string towerOrWing, int capacity)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "Slot code is required (e.g. A-12-3).";
            if (code.Trim().Length > 50)
                return "Slot code must be 50 characters or fewer.";
            if (string.IsNullOrWhiteSpace(towerOrWing))
                return "Tower / wing is required.";
            if (capacity < 1)
                return "Capacity must be at least 1 (use 1 for a true slot like A-12-3).";
            return null;
        }

        /// <summary>
        /// When code is blank, build A-12-3 from zone/aisle/tier.
        /// When code is present, leave it as the staff-facing slot address.
        /// </summary>
        internal static string ResolveSlotCode(string code, string zone, string aisle, int tier, string towerOrWing)
        {
            if (!string.IsNullOrWhiteSpace(code))
                return code.Trim().ToUpperInvariant();
            var zonePart = string.IsNullOrWhiteSpace(zone) ? towerOrWing : zone;
            return ShelfOccupancy.BuildSlotCode(zonePart, aisle, tier);
        }

        internal static bool CodesMatch(string left, string right) =>
            string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
