namespace HubManagement.Application.Auth.Services
{
    public sealed record PlatformAdminPasswordResetChallenge(
        Guid UserId,
        string Username,
        string Email,
        string CodeHash,
        int Attempts);
}
