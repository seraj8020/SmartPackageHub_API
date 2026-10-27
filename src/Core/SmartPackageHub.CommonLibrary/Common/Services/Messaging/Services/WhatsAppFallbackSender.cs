using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Models;
using SmartPackageHub.Common.Services.Messaging.Options;

namespace SmartPackageHub.Common.Services.Messaging.Services
{
    /// <summary>
    /// Multi-channel fallback when SMS fails. Posts to a configured WhatsApp gateway webhook when set;
    /// otherwise records a synthetic success for Development so the pipeline can be exercised.
    /// </summary>
    public sealed class WhatsAppFallbackSender : IWhatsAppSender
    {
        private readonly HttpClient _http;
        private readonly WhatsAppFallbackOptions _options;
        private readonly ILogger<WhatsAppFallbackSender> _logger;

        public WhatsAppFallbackSender(
            HttpClient http,
            IOptions<WhatsAppFallbackOptions> options,
            ILogger<WhatsAppFallbackSender> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// POST to configured webhook, or log-only in dev if URL empty
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!_options.Enabled)
                return new SmsSendResult(false, null, "WhatsApp fallback disabled.");

            if (string.IsNullOrWhiteSpace(_options.WebhookUrl))
            {
                _logger.LogWarning(
                    "WhatsApp fallback for {Destination} key {Key} (no WebhookUrl configured ? logged only).",
                    request.DestinationE164OrLocal,
                    request.IdempotencyKey);
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
