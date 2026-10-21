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

        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

        private static readonly Regex UnitRegex = new(
            @"^(?<tower>[A-Za-z0-9]{1,4})\s*[-/]\s*(?<flat>\d{2,4})$",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            RegexTimeout);

        public static void SplitUnit(ref string? tower, ref string? flat)
        {
            if (!string.IsNullOrWhiteSpace(tower) || string.IsNullOrWhiteSpace(flat))
                return;

            var match = UnitRegex.Match(flat!.Trim());
            if (!match.Success)
                return;

            tower = match.Groups["tower"].Value;
            flat = match.Groups["flat"].Value;
        }
    }
}
