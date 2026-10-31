using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Common.ConfigOptions;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Guards;
using SmartPackageHub.Common.Services.Messaging.Models;

namespace SmartPackageHub.Common.Services.Messaging.Services
{
    /// <summary>
    /// HTTP client for Fast2SMS /dev/bulkV2.
    /// </summary>
    public sealed class Fast2SmsClient : ISmsSender
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly Action<ILogger, Exception?> LogDisabled =
            LoggerMessage.Define(
                LogLevel.Warning,
                new EventId(1, nameof(Fast2SmsClient)),
                "Fast2SMS disabled or ApiKey missing; treating as submitted for local/dev.");

        private static readonly Action<ILogger, string, Exception?> LogNonJson =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(2, nameof(Fast2SmsClient)),
                "Fast2SMS returned non-JSON body: {Body}");

        private readonly HttpClient _http;
        private readonly Fast2SmsOptions _options;
        private readonly ILogger<Fast2SmsClient> _logger;

        public Fast2SmsClient(HttpClient http, IOptions<Fast2SmsOptions> options, ILogger<Fast2SmsClient> logger)
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

            if (!_options.Enabled
                || string.IsNullOrWhiteSpace(_options.ApiKey)
                || _options.ApiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
            {
                LogDisabled(_logger, null);
                return new SmsSendResult(true, $"dev-{request.IdempotencyKey}", null);
            }

            if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.DestinationE164OrLocal, out var numbers))
                return new SmsSendResult(false, null, "Invalid India mobile number.");

            using var message = new HttpRequestMessage(HttpMethod.Post, "/dev/bulkV2");
            message.Headers.TryAddWithoutValidation("Authorization", _options.ApiKey);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = BuildPayload(request, numbers);
            message.Content = new FormUrlEncodedContent(payload);

            using var response = await _http.SendAsync(message, cancellationToken).ConfigureAwait(false);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Fast2SmsApiResponse? parsed = null;
            try
            {
                parsed = JsonSerializer.Deserialize<Fast2SmsApiResponse>(raw, JsonOptions);
            }
            catch (JsonException ex)
            {
                LogNonJson(_logger, raw, ex);
            }

            if (!response.IsSuccessStatusCode || parsed is null || !parsed.Return)
            {
                var err = parsed?.Message is { Count: > 0 } msgs
                    ? string.Join("; ", msgs)
                    : $"HTTP {(int)response.StatusCode}: {raw}";
                return new SmsSendResult(false, parsed?.RequestId, err);
            }

            return new SmsSendResult(true, parsed.RequestId, null);
        }

        private Dictionary<string, string> BuildPayload(SmsSendRequest request, string numbers)
        {
            var route = string.IsNullOrWhiteSpace(_options.Route) ? "q" : _options.Route.Trim();
            var map = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["route"] = route,
                ["numbers"] = numbers,
                ["flash"] = "0",
                ["sms_details"] = "1",
                ["udf1"] = request.IdempotencyKey
            };

            if (request.UserDefinedFields != null)
            {
                foreach (var pair in request.UserDefinedFields)
                    map[pair.Key] = pair.Value;
            }

            if (string.Equals(route, "dlt", StringComparison.OrdinalIgnoreCase)
                || string.Equals(route, "dlt_manual", StringComparison.OrdinalIgnoreCase))
            {
                map["sender_id"] = request.SenderId ?? _options.SenderId;
                map["message"] = _options.DltMessageId;
                map["variables_values"] = ExtractOtpHint(request.MessageBody);
            }
            else
            {
                map["message"] = request.MessageBody;
            }

            return map;
        }

        private static string ExtractOtpHint(string body)
        {
            for (var i = 0; i <= body.Length - 6; i++)
            {
                var allDigits = true;
                for (var j = 0; j < 6; j++)
                {
                    if (!char.IsDigit(body[i + j]))
                    {
                        allDigits = false;
                        break;
                    }
                }

                if (allDigits)
                    return body.Substring(i, 6);
            }

            return body.Length <= 30 ? body : body[..30];
        }
    }
}
