using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartPackageHub_API.Models
{
    public class Resident
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public bool IsMember { get; set; } = false;

        public List<Package> Packages { get; set; } = new();
    }
}
