namespace HubManagement.Application.Auth.Services
{
    public sealed record PlatformAdminMfaChallenge(
        Guid UserId,
        string Username,
        string Email,
        string CodeHash,
        int Attempts);
}
