using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Common.ConfigOptions;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Models;

namespace SmartPackageHub.Common.Services.Messaging.Services
{
    /// <summary>
    /// Multi-channel fallback when SMS fails. Posts to a configured WhatsApp gateway webhook when set;
    /// otherwise records a synthetic success for Development so the pipeline can be exercised.
    /// </summary>
    public sealed class WhatsAppFallbackSender : IWhatsAppSender
    {
        private static readonly Action<ILogger, string, string, Exception?> LogDevFallback =
            LoggerMessage.Define<string, string>(
                LogLevel.Warning,
                new EventId(1, nameof(WhatsAppFallbackSender)),
                "WhatsApp fallback for {Destination} key {Key} (no WebhookUrl configured — logged only).");

        private readonly HttpClient _http;
        private readonly WhatsAppFallbackOptions _options;
        private readonly ILogger<WhatsAppFallbackSender> _logger;

        public WhatsAppFallbackSender(
            HttpClient http,
            IOptions<WhatsAppFallbackOptions> options,
            ILogger<WhatsAppFallbackSender> logger)
        {
            ArgumentNullException.ThrowIfNull(http);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!_options.Enabled)
                return new SmsSendResult(false, null, "WhatsApp fallback disabled.");

            if (_options.WebhookUrl is null)
            {
                LogDevFallback(_logger, request.DestinationE164OrLocal, request.IdempotencyKey, null);
                return new SmsSendResult(true, $"wa-dev-{request.IdempotencyKey}", null);
            }

            var body = new WhatsAppPayload(
                request.DestinationE164OrLocal,
                request.MessageBody,
                request.IdempotencyKey);

            using var response = await _http.PostAsJsonAsync(_options.WebhookUrl, body, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return new SmsSendResult(false, null, $"WhatsApp gateway HTTP {(int)response.StatusCode}: {err}");
            }

            return new SmsSendResult(true, $"wa-{request.IdempotencyKey}", null);
        }
    }
}
