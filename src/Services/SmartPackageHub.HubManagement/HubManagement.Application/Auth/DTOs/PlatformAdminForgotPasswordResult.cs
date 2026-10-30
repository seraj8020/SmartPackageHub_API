namespace HubManagement.Application.Auth.DTOs
{
    public sealed record PlatformAdminForgotPasswordResult(
        bool Succeeded,
        string? ResetToken,
        string? EmailHint,
        string? DevelopmentCode,
        string Message);
}
