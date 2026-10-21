using Microsoft.Extensions.Caching.Distributed;
using System.Buffers;
using System.Text.Json;

namespace SmartPackageHub.Common.Cache
{
    public class CacheService(IDistributedCache cache) : ICacheService
    {
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => cache.RemoveAsync(key, cancellationToken);

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var bytes = await cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
            return bytes is null ? default : Deserialize<T>(bytes);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            var bytes = Serialize(value);

            if (expiration is null)
            {
                await cache.SetAsync(key, bytes, cancellationToken).ConfigureAwait(false);
                return;
            }

            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration };
            await cache.SetAsync(key, bytes, options, cancellationToken).ConfigureAwait(false);
        }

        private static T Deserialize<T>(byte[] bytes)
        => JsonSerializer.Deserialize<T>(bytes)!;

        private static byte[] Serialize<T>(T value)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            using var jsonWriter = new Utf8JsonWriter(bufferWriter);

            JsonSerializer.Serialize(jsonWriter, value);

            return bufferWriter.WrittenSpan.ToArray();
        }
    }
}
