using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartPackageHub_API.Models
{
    public class DeliveryHistory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]  
        public string? PkgNumber { get; set; }
        public Guid PackageId { get; set; }
        public string? Notes { get; set; }
        public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;

        public string? Courier { get; set; }
        public string? Category { get; set; }

        public string MethodOfDelivery { get; set; }

        // Not mapped to the database; used by the UI. If not set, falls back to DeliveredAt.
        [NotMapped]
        [JsonPropertyName("collectedAt")]
        public DateTime CollectedAt
        {
            get => _collectedAt ?? DeliveredAt;
            set => _collectedAt = value;
        }
        private DateTime? _collectedAt;
    }
}
