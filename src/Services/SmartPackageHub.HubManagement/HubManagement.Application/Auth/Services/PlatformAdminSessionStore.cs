using Microsoft.Extensions.Caching.Memory;

namespace HubManagement.Application.Auth.Services
{
    public sealed class PlatformAdminSessionStore
    {
        public static readonly TimeSpan MfaTtl = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan SessionTtl = TimeSpan.FromHours(8);

        private readonly IMemoryCache _cache;

        public PlatformAdminSessionStore(IMemoryCache cache) => _cache = cache;

        public void SetMfaChallenge(string mfaToken, PlatformAdminMfaChallenge challenge) =>
            _cache.Set(MfaKey(mfaToken), challenge, MfaTtl);

        public bool TryGetMfaChallenge(string mfaToken, out PlatformAdminMfaChallenge challenge) =>
            _cache.TryGetValue(MfaKey(mfaToken), out challenge!);

        public void RemoveMfaChallenge(string mfaToken) => _cache.Remove(MfaKey(mfaToken));

        public void SetSession(string token, PlatformAdminSession session) =>
            _cache.Set(SessionKey(token), session, SessionTtl);

        public bool TryGetSession(string token, out PlatformAdminSession session) =>
            _cache.TryGetValue(SessionKey(token), out session!);

        public void RemoveSession(string token) => _cache.Remove(SessionKey(token));

        public void SetPasswordReset(string resetToken, PlatformAdminPasswordResetChallenge challenge) =>
            _cache.Set(ResetKey(resetToken), challenge, MfaTtl);

        public bool TryGetPasswordReset(string resetToken, out PlatformAdminPasswordResetChallenge challenge) =>
            _cache.TryGetValue(ResetKey(resetToken), out challenge!);

        public void RemovePasswordReset(string resetToken) => _cache.Remove(ResetKey(resetToken));

        private static string MfaKey(string token) => $"platform-admin-mfa:{token}";
        private static string SessionKey(string token) => $"platform-admin-session:{token}";
        private static string ResetKey(string token) => $"platform-admin-reset:{token}";
    }

    public sealed record PlatformAdminMfaChallenge(
        Guid UserId,
        string Username,
        string Email,
        string CodeHash,
        int Attempts);

    public sealed record PlatformAdminPasswordResetChallenge(
        Guid UserId,
        string Username,
        string Email,
        string CodeHash,
        int Attempts);

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
