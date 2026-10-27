using System.Text.Json.Serialization;

namespace SmartPackageHub.Common.Services.Messaging.Models
{
    public sealed class RecaptchaVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public double? Score { get; set; }
    }
}
