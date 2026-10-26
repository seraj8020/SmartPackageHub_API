
namespace SmartPackageHub.Common.Services.Messaging.Options
{
    public sealed class WhatsAppFallbackOptions
    {
        public const string SectionName = "WhatsAppFallback";

        public bool Enabled { get; set; } = true;

        /// <summary>Optional HTTP endpoint that accepts JSON { to, body, idempotencyKey }. When empty, fallback is logged only.</summary>
        public string? WebhookUrl { get; set; }
    }
}
