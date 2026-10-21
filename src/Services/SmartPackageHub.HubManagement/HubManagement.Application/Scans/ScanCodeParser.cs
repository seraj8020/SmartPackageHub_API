using System.Text.RegularExpressions;

namespace HubManagement.Application.Scans
{
    public static class ScanCodeParser
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

        private static readonly Regex AmazonTokenRegex = new(
            "[A-Z0-9]{6,}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            RegexTimeout);

        private static readonly Regex FlipkartTokenRegex = new(
            "[0-9]{6,}",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            RegexTimeout);

        public static string? ParseTracking(string? vendor, string? raw)
        {
            vendor = vendor?.Trim().ToUpperInvariant() ?? string.Empty;
            raw = raw?.Trim() ?? string.Empty;

            if (raw.Length >= 6 && raw.All(character => char.IsLetterOrDigit(character) || character == '-' || character == '_'))
                return raw;

            if (vendor.Contains("amazon", StringComparison.OrdinalIgnoreCase))
            {
                var tokens = AmazonTokenRegex.Matches(raw);
                if (tokens.Count > 0)
                    return tokens[0].Value;
            }

            if (vendor.Contains("flipkart", StringComparison.OrdinalIgnoreCase))
            {
                var tokens = FlipkartTokenRegex.Matches(raw);
                if (tokens.Count > 0)
                    return tokens[0].Value;
            }

            return string.IsNullOrWhiteSpace(raw) ? null : raw;
        }
    }
}
