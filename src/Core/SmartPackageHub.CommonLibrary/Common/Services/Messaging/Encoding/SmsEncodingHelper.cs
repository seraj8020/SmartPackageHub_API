using System.Text;

namespace SmartPackageHub.Common.Services.Messaging.Encoding
{
    /// <summary>
    /// GSM 7-bit vs UCS-2 awareness so OTP messages stay single-segment and to make cheaper rather than multi-segment costly.
    /// </summary>
    public static class SmsEncodingHelper
    {
        // Basic GSM 7-bit character set (no extension table for simplicity).
        private static readonly HashSet<char> Gsm7 = BuildGsm7();

        /// <summary>
        /// all chars in GSM-7 set?
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsGsm7Compatible(string text) =>
            text.All(Gsm7.Contains);

        /// <summary>
        /// replace ₹/smart quotes; non-GSM 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ToGsm7SafeOtpMessage(string text)
        {
            // Replace common non-GSM chars used in SPH copy so we stay on 160-char GSM segments.
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

        /// <summary>
        /// 160/153 GSM or 70/67 UCS-2 segments
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
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

        /// <summary>
        /// GSM character set
        /// </summary>
        /// <returns></returns>
        private static HashSet<char> BuildGsm7()
        {
            const string chars =
                "@£$¥èéùìòÇ\nØø\rÅå?_FG?O??ST? ÆæßÉ !\"#¤%&'()*+,-./0123456789:;<=>?" +
                "¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà";
            return [.. chars];
        }
    }
}
