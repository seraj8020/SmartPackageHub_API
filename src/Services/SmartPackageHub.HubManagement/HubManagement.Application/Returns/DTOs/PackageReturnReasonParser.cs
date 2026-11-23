using HubManagement.Core.Hubs.Models;

namespace HubManagement.Application.Returns.DTOs
{
    public static class PackageReturnReasonParser
    {
        public static bool TryParse(string? raw, out PackageReturnReason reason)
        {
            reason = default;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var value = raw.Trim()
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal);
            if (Enum.TryParse(value, ignoreCase: true, out reason))
                return true;

            // Friendly aliases from product copy.
            if (value.Contains("like", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("expected", StringComparison.OrdinalIgnoreCase))
            {
                reason = PackageReturnReason.NotAsExpected;
                return true;
            }

            if (value.Contains("damaged", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("inside", StringComparison.OrdinalIgnoreCase))
            {
                reason = PackageReturnReason.DamagedInside;
                return true;
            }

            if (value.Contains("expir", StringComparison.OrdinalIgnoreCase))
            {
                reason = PackageReturnReason.Expired;
                return true;
            }

            if (value.Contains("technical", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("mechanical", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("fault", StringComparison.OrdinalIgnoreCase))
            {
                reason = PackageReturnReason.TechnicalFault;
                return true;
            }

            if (value.Equals("other", StringComparison.OrdinalIgnoreCase))
            {
                reason = PackageReturnReason.Other;
                return true;
            }

            return false;
        }
    }
}
