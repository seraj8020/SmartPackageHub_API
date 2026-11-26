using System.Text.Json.Serialization;

namespace HubManagement.Application.Support.DTOs
{
    public sealed class CreateSupportRequestDto
    {
        [JsonPropertyName("residentId")]
        public Guid ResidentId { get; set; }

        [JsonPropertyName("societyId")]
        public Guid SocietyId { get; set; }

        [JsonPropertyName("topic")]
        public string Topic { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("residentName")]
        public string? ResidentName { get; set; }

        [JsonPropertyName("residentPhone")]
        public string? ResidentPhone { get; set; }

        [JsonPropertyName("towerWing")]
        public string? TowerWing { get; set; }

        [JsonPropertyName("flatNumber")]
        public string? FlatNumber { get; set; }
    }
}
