using System.Text.Json.Serialization;

namespace HubManagement.Application.Support.DTOs
{
    public sealed class CreateSupportRequestDto
    {
        [JsonPropertyName("residentId")]
        public Guid ResidentId { get; set; }

        [JsonPropertyName("societyId")]
        public Guid SocietyId { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        /// <summary>Category label (Traizr Category). Also accepts legacy "topic".</summary>
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("topic")]
        public string Topic { get; set; } = string.Empty;

        [JsonPropertyName("urgency")]
        public string Urgency { get; set; } = "Normal";

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

        public string ResolvedCategory
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Category))
                    return Category;
                if (!string.IsNullOrWhiteSpace(Topic))
                    return Topic;
                return string.Empty;
            }
        }
    }
}
