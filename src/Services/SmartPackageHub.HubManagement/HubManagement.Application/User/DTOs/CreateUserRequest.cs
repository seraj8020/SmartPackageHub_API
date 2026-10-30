namespace HubManagement.Application.HubUsers.DTOs
{
    public sealed class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public bool IsAdmin { get; set; }
        public string? Status { get; set; }
        /// <summary>Optional. Required for staff/admin accounts that sign in with username/password.</summary>
        public string? Password { get; set; }
    }
}
