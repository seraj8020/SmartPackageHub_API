using System.Text.RegularExpressions;

namespace HubManagement.Application.Intake
{
    internal static class IntakeText
    {
        public static string? First(params string?[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

        public static string? Clean(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            return trimmed.Length <= max ? trimmed : trimmed[..max];
        }

        public static void SplitUnit(ref string? tower, ref string? flat)
        {
            if (!string.IsNullOrWhiteSpace(tower) || string.IsNullOrWhiteSpace(flat))
                return;

            var match = Regex.Match(flat!.Trim(), @"^([A-Za-z0-9]{1,4})\s*[-/]\s*(\d{2,4})$");
            if (!match.Success)
                return;

            tower = match.Groups[1].Value;
            flat = match.Groups[2].Value;
        }
    }
}
