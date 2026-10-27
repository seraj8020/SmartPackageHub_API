using System.Text.Json.Serialization;

namespace SmartPackageHub.Common.Services.Messaging.Models
{
    public sealed record WhatsAppPayload(
            [property: JsonPropertyName("to")] string To,
            [property: JsonPropertyName("body")] string Body,
            [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey);
}
