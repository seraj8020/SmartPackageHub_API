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

        /// <summary>
        /// OCR often glues/mangles names (FehenKh ≈ Rehan Khan). Match on compacted letters
        /// with a small edit distance so tower/flat can still hydrate from the resident.
        /// </summary>
        public static List<Resident> FuzzyByName(IEnumerable<Resident> residents, string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<Resident>();

            var needle = CompactLetters(query);
            if (needle.Length < 5)
                return new List<Resident>();

            // Flipkart OCR glue (FehenKh ↔ RehanKhan) often needs distance 3.
            var maxDistance = needle.Length >= 7 ? 3 : 2;

            return residents
                .Select(resident =>
                {
                    var hay = CompactLetters(resident.FullName);
                    var distance = hay.Length == 0 ? int.MaxValue : LetterEditDistance(needle, hay);
                    return (resident, distance, hay);
                })
                .Where(row =>
                    row.hay.Length >= 5 &&
                    (row.hay.Contains(needle, StringComparison.Ordinal) ||
                     needle.Contains(row.hay, StringComparison.Ordinal) ||
                     row.distance <= maxDistance))
                .OrderBy(row => row.distance)
                .ThenByDescending(row => row.resident.IsActive)
                .ThenBy(row => row.resident.FullName, StringComparer.OrdinalIgnoreCase)
                .Select(row => row.resident)
                .Take(8)
                .ToList();
        }

        private static string CompactLetters(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return string.Concat(value.Where(char.IsLetter).Select(char.ToLowerInvariant));
        }

        private static int LetterEditDistance(string a, string b)
        {
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            var prev = new int[b.Length + 1];
            var curr = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++)
                prev[j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                curr[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(
                        Math.Min(curr[j - 1] + 1, prev[j] + 1),
                        prev[j - 1] + cost);
                }

                (prev, curr) = (curr, prev);
            }

            return prev[b.Length];
        }
    }
}
