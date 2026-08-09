using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartPackageHub_API.Models
{
    public class Package
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string PkgNumber { get; set; }

        [Required]
        public string TrackingNumber { get; set; }

        public string? Description { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public string? Category { get; set; }
        public string? Weight { get; set; }

        public Guid? ResidentId { get; set; }
        public Resident? Resident { get; set; }
        public string? Courier { get; set; }

        public bool IsPickedUp { get; set; } = false;
    }
}
