using System.ComponentModel.DataAnnotations;

namespace SmartPackageHub_API.Models
{
    public class Society
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; }

        public string? Address { get; set; }

        public string? ContactEmail { get; set; }

        public string? Phone { get; set; }
    }
}
