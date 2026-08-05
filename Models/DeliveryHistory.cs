using System.ComponentModel.DataAnnotations;

namespace SmartPackageHub_API.Models
{
    public class DeliveryHistory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PackageId { get; set; }
        public string? Notes { get; set; }
        public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;
    }
}
