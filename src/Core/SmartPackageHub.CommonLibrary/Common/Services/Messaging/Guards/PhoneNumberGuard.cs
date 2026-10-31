using System.Text.RegularExpressions;

namespace SmartPackageHub.Common.Services.Messaging.Guards
{
    public static partial class PhoneNumberGuard
    {
        /// <summary>
        /// Restricts SMS pumping to India mobile numbers (+91 / 10-digit starting 6-9).
        /// </summary>
        public static bool TryNormalizeIndiaMobile(string? raw, out string tenDigitLocal)
        {
            tenDigitLocal = string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var digits = DigitsOnly().Replace(raw, string.Empty);
            if (digits.StartsWith("91", StringComparison.Ordinal) && digits.Length == 12)
                digits = digits[2..];
            if (digits.StartsWith('0') && digits.Length == 11)
                digits = digits[1..];

            if (digits.Length != 10)
                return false;
            if (digits[0] is < '6' or > '9')
                return false;

            tenDigitLocal = digits;
            return true;
        }

        // Bound match time — digit stripping is linear and not ReDoS-prone, but analyzers require a timeout.
        [GeneratedRegex(@"\D", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 250)]
        private static partial Regex DigitsOnly();
    }
}
