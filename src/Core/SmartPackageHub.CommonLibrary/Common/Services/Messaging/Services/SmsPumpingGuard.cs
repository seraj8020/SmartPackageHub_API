using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Common.Cache;

using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Guards;
using SmartPackageHub.Common.Services.Messaging.Models;
using SmartPackageHub.Common.Services.Messaging.Options;

namespace SmartPackageHub.Common.Services.Messaging.Services
{
    public sealed class SmsPumpingGuard : ISmsPumpingGuard
    {
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
            _cache = cache;
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// India phone, optional reCAPTCHA, IP/phone Redis quotas
        /// </summary>
        /// <param name="clientIp"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="recaptchaToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// increment counters with TTL
        /// </summary>
        /// <param name="clientIp"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Google siteverify (v2/v3 score)
        /// </summary>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> VerifyRecaptchaAsync(string? token, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_options.RecaptchaSecretKey))
                return false;

            try
            {
                var client = _httpClientFactory.CreateClient("Recaptcha");
                using var response = await client.PostAsync(
                    $"https://www.google.com/recaptcha/api/siteverify?secret={Uri.EscapeDataString(_options.RecaptchaSecretKey)}&response={Uri.EscapeDataString(token)}",
                    content: null,
                    cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return false;

                var body = await response.Content.ReadFromJsonAsync<RecaptchaVerifyResponse>(cancellationToken).ConfigureAwait(false);
                if (body is null || !body.Success)
                    return false;

                // v3 score; v2 leaves Score null ? treat as pass when Success.
                if (body.Score is double score && score < _options.RecaptchaMinScore)
                    return false;

                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "reCAPTCHA verification error");
                return false;
            }
        }

        
    }
}
