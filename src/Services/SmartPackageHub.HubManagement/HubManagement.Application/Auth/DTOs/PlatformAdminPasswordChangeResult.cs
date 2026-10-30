namespace HubManagement.Application.Auth.DTOs
{
    public sealed record PlatformAdminPasswordChangeResult(
        bool Succeeded,
        bool Unauthorized,
        string Message);
}
