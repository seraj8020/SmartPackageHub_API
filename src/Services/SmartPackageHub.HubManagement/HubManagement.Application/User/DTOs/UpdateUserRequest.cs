
namespace HubManagement.Application.HubUsers.DTOs
{
    public sealed class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public bool? IsAdmin { get; set; }
        public string? Status { get; set; }
        /// <summary>Optional societies to link (existing links are kept; listed ids are added if missing).</summary>
        public IReadOnlyList<Guid>? SocietyIds { get; init; }
        public Guid? SocietyId { get; set; }
    }
}
