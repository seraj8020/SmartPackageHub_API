namespace HubManagement.Application.DeliverySlots.Helpers
{
    internal static class DeliverySlotWindows
    {
        public const int CapacityPerWindow = 5;
        public const string StatusBooked = "booked";
        public const string StatusDefault = "default";
        public const string StatusCancelled = "cancelled";

        /// <summary>System fallback when the resident does not pick a slot (6pm–9pm).</summary>
        public static readonly (TimeSpan Start, TimeSpan End, string Label) DefaultWindow =
            (TimeSpan.FromHours(18), TimeSpan.FromHours(21), "6–9");

        public static readonly (TimeSpan Start, TimeSpan End, string Label)[] DayWindows =
        [
            (TimeSpan.FromHours(9), TimeSpan.FromHours(12), "9–12"),
            (TimeSpan.FromHours(12), TimeSpan.FromHours(15), "12–3"),
            (TimeSpan.FromHours(15), TimeSpan.FromHours(18), "3–6"),
            DefaultWindow,
        ];

        public static bool TryResolveWindow(DateTime windowStart, DateTime windowEnd, out string label)
        {
            label = string.Empty;
            var start = windowStart.TimeOfDay;
            var end = windowEnd.TimeOfDay;
            foreach (var window in DayWindows)
            {
                if (window.Start == start && window.End == end)
                {
                    label = window.Label;
                    return true;
                }
            }

            return false;
        }

        public static string LabelFor(DateTime windowStart, DateTime windowEnd) =>
            TryResolveWindow(windowStart, windowEnd, out var label) ? label : $"{windowStart:HH:mm}–{windowEnd:HH:mm}";

        /// <summary>
        /// Current/next 6pm–9pm window in UTC (same convention as other delivery windows).
        /// Today's 6–9 stays valid until 9pm; after that, roll to tomorrow.
        /// </summary>
        public static (DateTime Start, DateTime End) NextDefaultWindow(DateTime utcNow)
        {
            var day = DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);
            var start = day.Add(DefaultWindow.Start);
            var end = day.Add(DefaultWindow.End);
            if (utcNow >= end)
            {
                start = start.AddDays(1);
                end = end.AddDays(1);
            }

            return (start, end);
        }

        public static bool IsActiveStatus(string status) =>
            !string.Equals(status, StatusCancelled, StringComparison.OrdinalIgnoreCase);

        public static bool IsSystemDefault(string status) =>
            string.Equals(status, StatusDefault, StringComparison.OrdinalIgnoreCase);
    }
}
