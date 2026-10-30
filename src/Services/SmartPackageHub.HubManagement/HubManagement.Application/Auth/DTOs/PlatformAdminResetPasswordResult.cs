namespace HubManagement.Application.Auth.DTOs
{
    public sealed record PlatformAdminResetPasswordResult(
        bool Succeeded,
        string Message);
}
