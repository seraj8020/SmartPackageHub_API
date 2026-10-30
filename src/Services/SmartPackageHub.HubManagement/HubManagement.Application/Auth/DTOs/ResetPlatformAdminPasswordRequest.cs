namespace HubManagement.Application.Auth.DTOs
{
    public sealed class ResetPlatformAdminPasswordRequest
    {
        public string? ResetToken { get; set; }
        public string? Code { get; set; }
        public string? NewPassword { get; set; }
    }
}
