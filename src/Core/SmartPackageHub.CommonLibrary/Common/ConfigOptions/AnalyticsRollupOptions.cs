namespace SmartPackageHub.Common.ConfigOptions
{
    public sealed class AnalyticsRollupOptions
    {
        public const string SectionName = "AnalyticsRollup";

        /// <summary>When false, the background rollup worker does not run.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Hour (0–23) in UTC after which yesterday's rollup may run.
        /// Default 18 ≈ end of India hub day in UTC for IST evening.
        /// </summary>
        public int RunAfterHourUtc { get; set; } = 18;

        /// <summary>How often the worker wakes to check whether a rollup is due.</summary>
        public int CheckIntervalMinutes { get; set; } = 30;
    }
}
