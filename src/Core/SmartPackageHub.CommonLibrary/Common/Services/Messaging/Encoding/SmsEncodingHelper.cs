using System.Text;

namespace SmartPackageHub.Common.Services.Messaging.Encoding
{
    /// <summary>
    /// GSM 7-bit vs UCS-2 awareness so OTP messages stay single-segment and cheaper than multi-segment.
    /// </summary>
    public static class SmsEncodingHelper
    {
        private static readonly HashSet<char> Gsm7 = BuildGsm7();

        public static bool IsGsm7Compatible(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            return text.All(Gsm7.Contains);
        }

        public static string ToGsm7SafeOtpMessage(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            var normalized = text
                .Replace("?", "Rs", StringComparison.Ordinal)
                .Replace("–", "-", StringComparison.Ordinal)
                .Replace("—", "-", StringComparison.Ordinal)
                .Replace("’", "'", StringComparison.Ordinal)
                .Replace("‘", "'", StringComparison.Ordinal)
                .Replace("“", "\"", StringComparison.Ordinal)
                .Replace("”", "\"", StringComparison.Ordinal);

            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                builder.Append(Gsm7.Contains(ch) ? ch : '?');
            }

            return builder.ToString();
        }

        public static int EstimateSegmentCount(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 1;

            if (IsGsm7Compatible(text))
            {
                if (text.Length <= 160) return 1;
                return (int)Math.Ceiling(text.Length / 153.0);
            }

            if (text.Length <= 70) return 1;
            return (int)Math.Ceiling(text.Length / 67.0);
        }

        private static HashSet<char> BuildGsm7()
        {
            const string chars =
                "@£$¥èéùìòÇ\nØø\rÅå?_FG?O??ST? ÆæßÉ !\"#¤%&'()*+,-./0123456789:;<=>?" +
                "¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà";
            return [.. chars];
        }
    }
}
