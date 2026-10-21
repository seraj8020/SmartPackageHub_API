namespace SmartPackageHub.Common.Types
{
    /// <summary>
    /// Represents application settings.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the Keycloak object.
        /// </summary>
        public required Keycloak Keycloak { get; set; }
    }
}
