using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Common.ConfigOptions;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Models;

namespace SmartPackageHub.Common.Services.Messaging.Services
{
    /// <summary>
    /// Sends email via Zoho CPaaS HTTP API and/or SMTP based on <see cref="EmailOptions.Transport"/>.
    /// When Email:Enabled is false, logs the message and succeeds (local/dev).
    /// </summary>
    public sealed class SmtpEmailSender : IEmailSender
    {
        private static readonly Action<ILogger, string?, Exception?> LogHttpFallback =
            LoggerMessage.Define<string?>(
                LogLevel.Warning,
                new EventId(1, nameof(SmtpEmailSender)),
                "Email HTTP failed ({Error}); trying SMTP fallback.");

        private static readonly Action<ILogger, string, string, string, Exception?> LogDevMode =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Information,
                new EventId(2, nameof(SmtpEmailSender)),
                "Email disabled/dev mode. To={To} Subject={Subject} Body={Body}");

        private static readonly Action<ILogger, int, string, Exception?> LogHttpFailed =
            LoggerMessage.Define<int, string>(
                LogLevel.Warning,
                new EventId(3, nameof(SmtpEmailSender)),
                "Email HTTP send failed Status={Status} Body={Body}");

        private static readonly Action<ILogger, string, string, Exception?> LogHttpSucceeded =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(4, nameof(SmtpEmailSender)),
                "Email HTTP send succeeded To={To} ProviderId={ProviderId}");

        private static readonly Action<ILogger, string, Exception?> LogHttpException =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(5, nameof(SmtpEmailSender)),
                "Email HTTP send failed to {To}");

        private static readonly Action<ILogger, Exception?> LogPort465 =
            LoggerMessage.Define(
                LogLevel.Warning,
                new EventId(6, nameof(SmtpEmailSender)),
                "Email SmtpPort 465 is not supported by SmtpClient; using 587 (STARTTLS) instead.");

        private static readonly Action<ILogger, string, Exception?> LogSmtpSucceeded =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(7, nameof(SmtpEmailSender)),
                "Email SMTP send succeeded To={To}");

        private static readonly Action<ILogger, string, Exception?> LogSmtpException =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(8, nameof(SmtpEmailSender)),
                "SMTP send failed to {To}");

        private readonly EmailOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IOptions<EmailOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<SmtpEmailSender> logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(httpClientFactory);
            ArgumentNullException.ThrowIfNull(logger);
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.ToAddress))
                return new EmailSendResult(false, null, "Recipient email is required.");

            if (!_options.Enabled)
                return LogDev(request);

            if (_options.PreferHttpThenSmtp)
            {
                var http = await SendViaHttpAsync(request, cancellationToken).ConfigureAwait(false);
                if (http.Succeeded)
                    return http;

                LogHttpFallback(_logger, http.ErrorMessage, null);
                var smtp = await SendViaSmtpAsync(request, cancellationToken).ConfigureAwait(false);
                if (smtp.Succeeded)
                    return smtp;

                return new EmailSendResult(
                    false,
                    null,
                    $"HTTP: {http.ErrorMessage}; SMTP: {smtp.ErrorMessage}");
            }

            if (_options.UseHttp)
                return await SendViaHttpAsync(request, cancellationToken).ConfigureAwait(false);

            if (_options.UseSmtp)
                return await SendViaSmtpAsync(request, cancellationToken).ConfigureAwait(false);

            return new EmailSendResult(false, null, $"Unsupported Email:Transport '{_options.Transport}'.");
        }

        private EmailSendResult LogDev(EmailSendRequest request)
        {
            LogDevMode(_logger, request.ToAddress, request.Subject, request.PlainTextBody, null);
            var id = request.IdempotencyKey ?? Guid.NewGuid().ToString("N");
            return new EmailSendResult(true, $"dev-email-{id}", null);
        }

        private async Task<EmailSendResult> SendViaHttpAsync(EmailSendRequest request, CancellationToken cancellationToken)
        {
            if (_options.HttpUrl is null || string.IsNullOrWhiteSpace(_options.HttpApiKey))
                return new EmailSendResult(false, null, "Email HTTP transport requires HttpUrl and HttpApiKey.");

            try
            {
                var payload = new
                {
                    from = new
                    {
                        address = _options.FromAddress,
                        name = _options.FromDisplayName
                    },
                    to = new[]
                    {
                        new
                        {
                            email_address = new
                            {
                                address = request.ToAddress.Trim()
                            }
                        }
                    },
                    subject = request.Subject,
                    textbody = request.PlainTextBody,
                    client_reference = request.IdempotencyKey
                };

                using var message = new HttpRequestMessage(HttpMethod.Post, _options.HttpUrl);
                message.Headers.TryAddWithoutValidation("Authorization", $"Zoho-enczapikey {_options.HttpApiKey}");
                message.Headers.TryAddWithoutValidation("Accept", "application/json");
                message.Content = JsonContent.Create(payload);

                var client = _httpClientFactory.CreateClient("EmailHttp");
                using var response = await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    LogHttpFailed(_logger, (int)response.StatusCode, body, null);
                    return new EmailSendResult(false, null, $"HTTP {(int)response.StatusCode}: {body}");
                }

                var providerId = TryReadRequestId(body) ?? Guid.NewGuid().ToString("N");
                LogHttpSucceeded(_logger, request.ToAddress, providerId, null);
                return new EmailSendResult(true, providerId, null);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                LogHttpException(_logger, request.ToAddress, ex);
                return new EmailSendResult(false, null, ex.Message);
            }
        }

        private async Task<EmailSendResult> SendViaSmtpAsync(EmailSendRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.SmtpHost))
                return new EmailSendResult(false, null, "Email SMTP transport requires SmtpHost.");

            var port = _options.SmtpPort;
            if (port == 465)
            {
                LogPort465(_logger, null);
                port = 587;
            }

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_options.FromAddress, _options.FromDisplayName),
                    Subject = request.Subject,
                    Body = request.PlainTextBody,
                    IsBodyHtml = false
                };
                message.To.Add(request.ToAddress.Trim());

                using var client = new SmtpClient(_options.SmtpHost, port)
                {
                    EnableSsl = _options.UseSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };
                if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
                {
                    client.Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword);
                }

                await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
                LogSmtpSucceeded(_logger, request.ToAddress, null);
                return new EmailSendResult(true, Guid.NewGuid().ToString("N"), null);
            }
            catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException or ArgumentException)
            {
                LogSmtpException(_logger, request.ToAddress, ex);
                return new EmailSendResult(false, null, ex.Message);
            }
        }

        private static string? TryReadRequestId(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("request_id", out var id))
                    return id.GetString();
            }
            catch (JsonException)
            {
                // Provider body was not JSON; provider id stays synthetic.
            }

            return null;
        }
    }
}
