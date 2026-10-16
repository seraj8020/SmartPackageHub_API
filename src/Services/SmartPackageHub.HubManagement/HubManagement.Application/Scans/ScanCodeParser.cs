using System.Text.RegularExpressions;

namespace HubManagement.Application.Scans
{
    public static class ScanCodeParser
    {
        public static string? ParseTracking(string? vendor, string? raw)
        {
            vendor = vendor?.Trim().ToLowerInvariant() ?? string.Empty;
            raw = raw?.Trim() ?? string.Empty;

            if (raw.Length >= 6 && raw.All(character => char.IsLetterOrDigit(character) || character == '-' || character == '_'))
                return raw;

            if (vendor.Contains("amazon"))
            {
                var tokens = Regex.Matches(raw, "[A-Z0-9]{6,}", RegexOptions.IgnoreCase);
                if (tokens.Count > 0)
                    return tokens[0].Value;
            }

            if (vendor.Contains("flipkart"))
            {
                var tokens = Regex.Matches(raw, "[0-9]{6,}");
                if (tokens.Count > 0)
                    return tokens[0].Value;
            }

            return string.IsNullOrWhiteSpace(raw) ? null : raw;
        }
    }
}
