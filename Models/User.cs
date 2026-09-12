using System.ComponentModel.DataAnnotations;

namespace SmartPackageHub_API.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Username { get; set; }

        [Required]
        public string Email { get; set; }

        public string? FullName { get; set; }

        public bool IsAdmin { get; set; } = false;

        // For demo purposes we store a plain password field. In production this should be a salted hash.
        public string? Password { get; set; }
    }
}
