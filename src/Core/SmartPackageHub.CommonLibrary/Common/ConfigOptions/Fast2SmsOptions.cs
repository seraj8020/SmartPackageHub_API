namespace SmartPackageHub.Common.ConfigOptions
{
    public sealed class Fast2SmsOptions
    {
        public const string SectionName = "Fast2Sms";

        /// <summary>Authorization header value from Fast2SMS dashboard (use test/sandbox key in Development).</summary>
        public string ApiKey { get; set; } = string.Empty;

        public Uri BaseUrl { get; set; } = new("https://www.fast2sms.com");

        /// <summary>Quick SMS route "q" for test keys; use "dlt" in production with SenderId + MessageId.</summary>
        public string Route { get; set; } = "q";

        /// <summary>DLT-approved Sender ID (e.g. SHPOTP). Required when Route is dlt.</summary>
        public string SenderId { get; set; } = "SHPOTP";

        /// <summary>DLT template Message ID. Required when Route is dlt.</summary>
        public string DltMessageId { get; set; } = string.Empty;

        /// <summary>Max outbound SMS submissions per second (provider throughput).</summary>
        public double MaxSendsPerSecond { get; set; } = 1;

        public bool Enabled { get; set; } = true;
    }
}
