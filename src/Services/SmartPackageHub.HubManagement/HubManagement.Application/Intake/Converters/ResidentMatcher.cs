using HubManagement.Application.Intake.Helpers;
using HubManagement.Application.Intake.DTOs;
using HubManagement.Core.Hubs.Entities;

namespace HubManagement.Application.Intake.Converters
{
    internal static class ResidentMatcher
    {
        public static ResidentMatchDto ToDto(Resident resident)
        {
            ArgumentNullException.ThrowIfNull(resident);
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
                AvatarUrl = Uri.TryCreate(resident.AvatarUrl, UriKind.Absolute, out Uri? result)? result: null,
                IsActive = resident.IsActive,
                PhoneNumber = resident.PhoneNumber,
                Unit = unit
            };
        }

        public static bool TowerEquals(string? residentTower, string? wanted)
        {
            if (string.IsNullOrWhiteSpace(wanted) || string.IsNullOrWhiteSpace(residentTower))
                return false;

            // Created a reusable reference to the equality comparer
            var comparer = StringComparer.OrdinalIgnoreCase;

            if (comparer.Equals(residentTower.Trim(), wanted.Trim()))
                return true;

            var cleanedResident = LabelTextParser.CleanTower(residentTower);
            var cleanedWanted = LabelTextParser.CleanTower(wanted) ?? wanted.Trim();
            if (cleanedResident != null && comparer.Equals(cleanedResident, cleanedWanted))
                return true;

            var last = residentTower.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            return last != null && comparer.Equals(last, cleanedWanted);
        }

        public static bool FlatEquals(string? residentFlat, string? wanted)
        {
            if (string.IsNullOrWhiteSpace(wanted) || string.IsNullOrWhiteSpace(residentFlat))
                return false;

            var comparer = StringComparer.OrdinalIgnoreCase;

            if (comparer.Equals(residentFlat.Trim(), wanted.Trim()))
                return true;

            var cleanedResident = LabelTextParser.CleanFlat(residentFlat);
            var cleanedWanted = LabelTextParser.CleanFlat(wanted) ?? wanted.Trim();
            return cleanedResident != null && comparer.Equals(cleanedResident, cleanedWanted);
        }

        public static List<Resident> ByTowerAndFlat(IEnumerable<Resident> residents, string tower, string flat) =>
            residents
                .Where(resident => TowerEquals(resident.TowerWing, tower) && FlatEquals(resident.Unit?.Value, flat))
                .OrderByDescending(resident => resident.IsActive)
                .ThenBy(resident => resident.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();
       
        public static List<Resident> Search(IEnumerable<Resident> residents, string? query)
        {
            // If query is null or whitespace, we can avoid further processing
            if (string.IsNullOrWhiteSpace(query))
            {
                return residents.OrderBy(resident => resident.FullName, StringComparer.OrdinalIgnoreCase)
                                .Take(8)
                                .ToList();
            }

            var term = query.Trim();
            var rows = residents.OrderBy(resident => resident.FullName, StringComparer.OrdinalIgnoreCase);

            return rows.Where(resident =>
                    (resident.FullName != null && resident.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (resident.Unit?.Value != null && resident.Unit.Value.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (resident.TowerWing != null && resident.TowerWing.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (resident.PhoneNumber != null && resident.PhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    ($"{resident.TowerWing}-{resident.Unit?.Value}").Contains(term, StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .ToList();
        }
    }
}
