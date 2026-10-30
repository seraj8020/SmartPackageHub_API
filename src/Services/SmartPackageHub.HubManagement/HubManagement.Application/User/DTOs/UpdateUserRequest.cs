
namespace HubManagement.Application.HubUsers.DTOs
{
    public sealed class UpdateUserRequest
    {
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public bool? IsAdmin { get; set; }
        public string? Status { get; set; }
    }
}
