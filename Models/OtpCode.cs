using System.ComponentModel.DataAnnotations;

namespace SmartPackageHub_API.Models
{
    public class OtpCode
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ResidentId { get; set; }
        public string Code { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}
