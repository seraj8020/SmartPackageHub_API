using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Common.Cache;
using SmartPackageHub.Common.ConfigOptions;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Guards;
using SmartPackageHub.Common.Services.Messaging.Models;

namespace SmartPackageHub.Common.Services.Messaging.Services
{
    public sealed class SmsPumpingGuard : ISmsPumpingGuard
    {
        private static readonly Uri RecaptchaSiteVerifyUri =
            new("https://www.google.com/recaptcha/api/siteverify");

        private static readonly Action<ILogger, Exception?> LogRecaptchaError =
            LoggerMessage.Define(
                LogLevel.Warning,
                new EventId(1, nameof(SmsPumpingGuard)),
                "reCAPTCHA verification error");

        private readonly ICacheService _cache;
        private readonly SmsPumpingProtectionOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SmsPumpingGuard> _logger;

        public SmsPumpingGuard(
            ICacheService cache,
            IOptions<SmsPumpingProtectionOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<SmsPumpingGuard> logger)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(httpClientFactory);
            ArgumentNullException.ThrowIfNull(logger);
            _cache = cache;
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<SmsPumpingCheckResult> CheckAsync(
            string? clientIp,
            string phoneNumber,
            string? recaptchaToken,
            CancellationToken cancellationToken = default)
        {
            if (_options.RestrictToIndia && !PhoneNumberGuard.TryNormalizeIndiaMobile(phoneNumber, out _))
                return new SmsPumpingCheckResult(false, "SMS is restricted to India (+91) mobile numbers.");

            if (_options.RecaptchaRequired)
            {
                var captchaOk = await VerifyRecaptchaAsync(recaptchaToken, cancellationToken).ConfigureAwait(false);
                if (!captchaOk)
                    return new SmsPumpingCheckResult(false, "reCAPTCHA verification failed.");
            }

            var ipKey = $"sms-pump:ip:{clientIp ?? "unknown"}";
            var phoneKey = $"sms-pump:phone:{phoneNumber}";

            var ipCount = await _cache.GetAsync<int?>(ipKey, cancellationToken).ConfigureAwait(false) ?? 0;
            if (ipCount >= _options.MaxRequestsPerIp)
                return new SmsPumpingCheckResult(false, "Too many OTP SMS requests from this IP. Try again later.");

            var phoneCount = await _cache.GetAsync<int?>(phoneKey, cancellationToken).ConfigureAwait(false) ?? 0;
            if (phoneCount >= _options.MaxRequestsPerPhone)
                return new SmsPumpingCheckResult(false, "Too many OTP SMS requests for this number. Try again later.");

            return new SmsPumpingCheckResult(true, null);
        }

        public async Task RecordSuccessAsync(string? clientIp, string phoneNumber, CancellationToken cancellationToken = default)
        {
            var ipWindow = TimeSpan.FromMinutes(Math.Max(1, _options.IpWindowMinutes));
            var phoneWindow = TimeSpan.FromMinutes(Math.Max(1, _options.PhoneWindowMinutes));

            var ipKey = $"sms-pump:ip:{clientIp ?? "unknown"}";
            var phoneKey = $"sms-pump:phone:{phoneNumber}";

            var ipCount = await _cache.GetAsync<int?>(ipKey, cancellationToken).ConfigureAwait(false) ?? 0;
            await _cache.SetAsync(ipKey, ipCount + 1, ipWindow, cancellationToken).ConfigureAwait(false);

            var phoneCount = await _cache.GetAsync<int?>(phoneKey, cancellationToken).ConfigureAwait(false) ?? 0;
            await _cache.SetAsync(phoneKey, phoneCount + 1, phoneWindow, cancellationToken).ConfigureAwait(false);
        }

        private async Task<bool> VerifyRecaptchaAsync(string? token, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_options.RecaptchaSecretKey))
                return false;

            try
            {
                var client = _httpClientFactory.CreateClient("Recaptcha");
                using var content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("secret", _options.RecaptchaSecretKey),
                    new KeyValuePair<string, string>("response", token),
                ]);
                using var response = await client.PostAsync(RecaptchaSiteVerifyUri, content, cancellationToken)
                    .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return false;

                var body = await response.Content.ReadFromJsonAsync<RecaptchaVerifyResponse>(cancellationToken)
                    .ConfigureAwait(false);
                if (body is null || !body.Success)
                    return false;

                if (body.Score is double score && score < _options.RecaptchaMinScore)
                    return false;

                return true;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
            {
                LogRecaptchaError(_logger, ex);
                return false;
            }
        }
    }
}
