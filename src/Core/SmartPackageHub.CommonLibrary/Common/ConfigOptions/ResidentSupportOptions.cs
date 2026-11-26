namespace SmartPackageHub.Common.ConfigOptions
{
    /// <summary>
    /// Resident in-app Support destination. Override per environment in appsettings.
    /// </summary>
    public sealed class ResidentSupportOptions
    {
        public const string SectionName = "ResidentSupport";

        /// <summary>Inbox that receives resident support requests (mailto + API routing).</summary>
        public string SupportEmail { get; set; } = "hello@byonverse.com";

        /// <summary>Optional phone shown in the resident Support screen.</summary>
        public string? SupportPhone { get; set; }
    }
}
