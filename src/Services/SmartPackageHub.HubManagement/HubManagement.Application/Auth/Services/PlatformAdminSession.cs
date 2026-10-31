namespace HubManagement.Application.Auth.Services
{
    /// <summary>
    /// Opaque admin session. Role is PlatformAdmin (all societies) or Admin (society-scoped via SocietyIds).
    /// </summary>
    public sealed record PlatformAdminSession(
        Guid UserId,
        string Username,
        string Email,
        string Role,
        IReadOnlyList<Guid> SocietyIds);
}
