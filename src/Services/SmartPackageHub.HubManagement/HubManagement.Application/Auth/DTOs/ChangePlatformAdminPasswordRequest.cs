namespace HubManagement.Application.Auth.DTOs
{
    public sealed class ChangePlatformAdminPasswordRequest
    {
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}
