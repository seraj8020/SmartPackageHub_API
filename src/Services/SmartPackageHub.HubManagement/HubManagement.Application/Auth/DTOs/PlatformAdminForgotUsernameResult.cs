namespace HubManagement.Application.Auth.DTOs
{
    public sealed record PlatformAdminForgotUsernameResult(
        bool Succeeded,
        string? Username,
        string? EmailHint,
        string Message);
}
