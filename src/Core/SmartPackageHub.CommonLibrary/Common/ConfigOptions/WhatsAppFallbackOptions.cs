namespace SmartPackageHub.Common.ConfigOptions
{
    public sealed class WhatsAppFallbackOptions
    {
        public const string SectionName = "WhatsAppFallback";

        public bool Enabled { get; set; } = true;

        /// <summary>Optional HTTP endpoint that accepts JSON { to, body, idempotencyKey }. When empty, fallback is logged only.</summary>
        public Uri? WebhookUrl { get; set; }
    }
}
