namespace SmartPackageHub.Common.Types
{
    /// <summary>
    /// Represents Keycloak configuration settings.
    /// </summary>
    public class Keycloak
    {
        /// <summary>
        /// Gets or sets the keycloak Base Url.
        /// </summary>
        public required Uri BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the keycloak Realm.
        /// </summary>
        public required string Realm { get; set; }

        /// <summary>
        /// Gets or sets the Keycloak client ID.
        /// </summary>
        public required string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the Keycloak client secret.
        /// </summary>
        public required string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the Keycloak audience.
        /// </summary>
        public required string Audience { get; set; }

        /// <summary>
        /// Gets or sets the Keycloak authority.
        /// </summary>
        public required string Authority { get; set; }
    }
}
