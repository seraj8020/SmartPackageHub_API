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
                return "Shelf code is required.";
            if (code.Trim().Length > 50)
                return "Shelf code must be 50 characters or fewer.";
            if (string.IsNullOrWhiteSpace(towerOrWing))
                return "Tower / wing is required.";
            if (capacity < 1)
                return "Capacity must be at least 1.";
            return null;
        }

        internal static bool CodesMatch(string left, string right) =>
            string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
