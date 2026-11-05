namespace SmartPackageHub.Common.ConfigOptions
{
    public sealed class AnalyticsRollupOptions
    {
        public const string SectionName = "AnalyticsRollup";

        /// <summary>When false, the background rollup worker does not run.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// UTC time after which today's rollup may run.
        /// Defaults to 12:30 UTC, which is 6:00 PM India time.
        /// </summary>
        public int RunAfterHourUtc { get; set; } = 12;

        public int RunAfterMinuteUtc { get; set; } = 30;

        /// <summary>How often the worker wakes to check whether a rollup is due.</summary>
        public int CheckIntervalMinutes { get; set; } = 5;
    }
}
