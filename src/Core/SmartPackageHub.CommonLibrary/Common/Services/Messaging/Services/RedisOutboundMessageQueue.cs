using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Models;
using SmartPackageHub.Common.ConfigOptions;

namespace SmartPackageHub.Common.Services.Messaging.Services
{
    /// <summary>
    /// Redis list buffer so sends are rate-limited.
    /// </summary>
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
            _redis = redis;
            _options = options.Value;
        }

        /// <summary>
        /// RPUSH JSON work item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task EnqueueAsync(OutboundMessageWorkItem item, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            var db = _redis.GetDatabase();
            var payload = JsonSerializer.Serialize(item, JsonOptions);
            await db.ListRightPushAsync(_options.RedisListKey, payload).ConfigureAwait(false);
        }

        /// <summary>
        /// LPOP + deserialize
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OutboundMessageWorkItem?> DequeueAsync(CancellationToken cancellationToken = default)
        {
            var db = _redis.GetDatabase();
            // BRPOP via polling ListLeftPop ? ConnectionMultiplexer timeout-friendly for hosted services.
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
