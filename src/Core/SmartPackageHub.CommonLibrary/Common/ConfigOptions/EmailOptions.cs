namespace SmartPackageHub.Common.ConfigOptions
{
    public sealed class EmailOptions
    {
        public const string SectionName = "Email";

        /// <summary>When false, emails are logged only (local/dev).</summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Delivery channel: <c>http</c>, <c>smtp</c>, or <c>both</c>
        /// (HTTP first, then SMTP fallback on failure).
        /// </summary>
        public string Transport { get; set; } = "both";

        public string FromAddress { get; set; } = "noreply@smartpackagehub.local";
        public string FromDisplayName { get; set; } = "Smart Package Hub";

        /// <summary>HTTP provider label (e.g. zeptomail).</summary>
        public string? HttpProvider { get; set; }

        /// <summary>Full POST URL, e.g. https://cpaas.zoho.in/v1.1/email</summary>
        public string? HttpUrl { get; set; }

        /// <summary>Agent Send Mail token (Authorization: Zoho-enczapikey …).</summary>
        public string? HttpApiKey { get; set; }

        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public string? SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; }
        public bool UseSsl { get; set; } = true;

        public bool UseHttp =>
            string.Equals(Transport, "http", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Transport, "both", StringComparison.OrdinalIgnoreCase);

        public bool UseSmtp =>
            string.Equals(Transport, "smtp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Transport, "both", StringComparison.OrdinalIgnoreCase);

        public bool PreferHttpThenSmtp =>
            string.Equals(Transport, "both", StringComparison.OrdinalIgnoreCase);
    }
}
