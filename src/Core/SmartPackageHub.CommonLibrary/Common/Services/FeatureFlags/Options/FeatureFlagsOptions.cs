namespace SmartPackageHub.Common.Services.FeatureFlags.Options
{
    /// <summary>
    /// Toggle product features from appsettings without a redeploy of UI logic.
    /// Add new boolean properties here and mirror them under "FeatureFlags" in appsettings.json.
    /// </summary>
    public sealed class FeatureFlagsOptions
    {
        public const string SectionName = "FeatureFlags";

        /// <summary>
        /// When true, staff can mark packages as COD and residents see approval UI.
        /// When false, COD intake/approve/reject are disabled and COD UI is hidden.
        /// </summary>
        public bool CashOnDelivery { get; set; } = true;
    }
}
