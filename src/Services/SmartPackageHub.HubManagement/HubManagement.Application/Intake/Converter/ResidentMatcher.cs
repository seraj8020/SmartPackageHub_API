using HubManagement.Application.Intake.DTOs;
using HubManagement.Core.Hubs.Entities;

namespace HubManagement.Application.Intake
{
    internal static class ResidentMatcher
    {
        public static ResidentMatchDto ToDto(Resident resident)
        {
            var flat = resident.Unit?.Value;
            var tower = resident.TowerWing;
            var unit = string.IsNullOrWhiteSpace(tower) ? flat ?? string.Empty : $"{tower}-{flat}";
            return new ResidentMatchDto
            {
                Id = resident.Id,
                SocietyId = resident.SocietyId,
                Name = resident.FullName,
                Tower = tower,
                FlatNumber = flat,
                AvatarUrl = resident.AvatarUrl,
                IsActive = resident.IsActive,
                PhoneNumber = resident.PhoneNumber,
                Unit = unit
            };
        }

        public static bool TowerEquals(string? residentTower, string? wanted)
        {
            if (string.IsNullOrWhiteSpace(wanted) || string.IsNullOrWhiteSpace(residentTower))
                return false;

            if (string.Equals(residentTower.Trim(), wanted.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            var cleanedResident = LabelTextParser.CleanTower(residentTower);
            var cleanedWanted = LabelTextParser.CleanTower(wanted) ?? wanted.Trim();
            if (cleanedResident != null && string.Equals(cleanedResident, cleanedWanted, StringComparison.OrdinalIgnoreCase))
                return true;

            var last = residentTower.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            return last != null && string.Equals(last, cleanedWanted, StringComparison.OrdinalIgnoreCase);
        }

        public static bool FlatEquals(string? residentFlat, string? wanted)
        {
            if (string.IsNullOrWhiteSpace(wanted) || string.IsNullOrWhiteSpace(residentFlat))
                return false;

            if (string.Equals(residentFlat.Trim(), wanted.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            var cleanedResident = LabelTextParser.CleanFlat(residentFlat);
            var cleanedWanted = LabelTextParser.CleanFlat(wanted) ?? wanted.Trim();
            return cleanedResident != null && string.Equals(cleanedResident, cleanedWanted, StringComparison.OrdinalIgnoreCase);
        }

        public static List<Resident> ByTowerAndFlat(IEnumerable<Resident> residents, string tower, string flat) =>
            residents
                .Where(resident => TowerEquals(resident.TowerWing, tower) && FlatEquals(resident.Unit?.Value, flat))
                .OrderByDescending(resident => resident.IsActive)
                .ThenBy(resident => resident.FullName)
                .ToList();

        public static List<Resident> Search(IEnumerable<Resident> residents, string? query)
        {
            var term = query?.Trim().ToLowerInvariant() ?? string.Empty;
            var rows = residents.OrderBy(resident => resident.FullName);
            if (term.Length == 0)
                return rows.Take(8).ToList();

            return rows.Where(resident =>
                    (resident.FullName ?? string.Empty).ToLowerInvariant().Contains(term) ||
                    (resident.Unit?.Value ?? string.Empty).ToLowerInvariant().Contains(term) ||
                    (resident.TowerWing ?? string.Empty).ToLowerInvariant().Contains(term) ||
                    (resident.PhoneNumber ?? string.Empty).Contains(term) ||
                    $"{resident.TowerWing}-{resident.Unit?.Value}".ToLowerInvariant().Contains(term))
                .Take(8)
                .ToList();
        }
    }
}
