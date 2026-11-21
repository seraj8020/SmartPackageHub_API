using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace HubManagement.Application.Auth.Services
{
    public sealed class StaffSessionStore
    {
        public static readonly TimeSpan SessionTtl = TimeSpan.FromDays(30);
        private readonly IDistributedCache _cache;

        public StaffSessionStore(IDistributedCache cache) => _cache = cache;

        public Task SetSessionAsync(
            string token,
            StaffSession session,
            CancellationToken cancellationToken = default) =>
            _cache.SetStringAsync(
                SessionKey(token),
                JsonSerializer.Serialize(session),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = SessionTtl },
                cancellationToken);

        public async Task<StaffSession?> GetSessionAsync(
            string token,
            CancellationToken cancellationToken = default)
        {
            var value = await _cache.GetStringAsync(SessionKey(token), cancellationToken).ConfigureAwait(false);
            return value is null ? null : JsonSerializer.Deserialize<StaffSession>(value);
        }

        private static string SessionKey(string token) => $"staff-session:{token}";
    }
}