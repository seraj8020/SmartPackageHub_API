using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartPackageHub.Common.ConfigOptions;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Models;
using StackExchange.Redis;

namespace SmartPackageHub.Common.Services.Messaging.Services
{
    /// <summary>
    /// Redis list buffer so sends are rate-limited.
    /// </summary>
    [SuppressMessage(
        "Naming",
        "CA1711:Identifiers should not have incorrect suffix",
        Justification = "Queue is intentional domain naming for the Redis-backed outbound work buffer.")]
    public sealed class RedisOutboundMessageQueue : IOutboundMessageQueue
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly IConnectionMultiplexer _redis;
        private readonly OutboundMessageQueueOptions _options;

        public RedisOutboundMessageQueue(IConnectionMultiplexer redis, IOptions<OutboundMessageQueueOptions> options)
        {
            ArgumentNullException.ThrowIfNull(redis);
            ArgumentNullException.ThrowIfNull(options);
            _redis = redis;
            _options = options.Value;
        }

        public async Task EnqueueAsync(OutboundMessageWorkItem item, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            var db = _redis.GetDatabase();
            var payload = JsonSerializer.Serialize(item, JsonOptions);
            await db.ListRightPushAsync(_options.RedisListKey, payload).ConfigureAwait(false);
        }

        public async Task<OutboundMessageWorkItem?> DequeueAsync(CancellationToken cancellationToken = default)
        {
            var db = _redis.GetDatabase();
            var value = await db.ListLeftPopAsync(_options.RedisListKey).ConfigureAwait(false);
            if (value.IsNullOrEmpty)
                return null;

            try
            {
                return JsonSerializer.Deserialize<OutboundMessageWorkItem>(value!, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
