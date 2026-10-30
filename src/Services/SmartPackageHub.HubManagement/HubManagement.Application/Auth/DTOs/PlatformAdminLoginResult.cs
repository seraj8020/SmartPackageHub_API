namespace HubManagement.Application.Auth.DTOs
{
    public sealed record PlatformAdminLoginResult(
        bool Succeeded,
        bool MfaRequired,
        string? MfaToken,
        string? EmailHint,
        string? DevelopmentCode,
        string? AccessToken,
        string Message,
        string? Role = null,
        string? Username = null,
        IReadOnlyList<Guid>? SocietyIds = null);
}
