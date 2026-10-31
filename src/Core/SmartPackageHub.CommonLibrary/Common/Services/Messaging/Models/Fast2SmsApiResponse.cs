using System.Text.Json.Serialization;

namespace SmartPackageHub.Common.Services.Messaging.Models
{
    public sealed class Fast2SmsApiResponse
    {
        [JsonPropertyName("return")]
        public bool Return { get; set; }

        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        [JsonPropertyName("message")]
        public IReadOnlyList<string>? Message { get; set; }
    }
}
