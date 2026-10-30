using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
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
        private readonly EmailOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IOptions<EmailOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<SmtpEmailSender> logger)
        {
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

                _logger.LogWarning(
                    "Email HTTP failed ({Error}); trying SMTP fallback.",
                    http.ErrorMessage);
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
            _logger.LogInformation(
                "Email disabled/dev mode. To={To} Subject={Subject} Body={Body}",
                request.ToAddress,
                request.Subject,
                request.PlainTextBody);
            var id = request.IdempotencyKey ?? Guid.NewGuid().ToString("N");
            return new EmailSendResult(true, $"dev-email-{id}", null);
        }

        private async Task<EmailSendResult> SendViaHttpAsync(EmailSendRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.HttpUrl) || string.IsNullOrWhiteSpace(_options.HttpApiKey))
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
                    _logger.LogWarning(
                        "Email HTTP send failed Status={Status} Body={Body}",
                        (int)response.StatusCode,
                        body);
                    return new EmailSendResult(false, null, $"HTTP {(int)response.StatusCode}: {body}");
                }

                var providerId = TryReadRequestId(body) ?? Guid.NewGuid().ToString("N");
                _logger.LogInformation(
                    "Email HTTP send succeeded To={To} ProviderId={ProviderId}",
                    request.ToAddress,
                    providerId);
                return new EmailSendResult(true, providerId, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email HTTP send failed to {To}", request.ToAddress);
                return new EmailSendResult(false, null, ex.Message);
            }
        }

        private async Task<EmailSendResult> SendViaSmtpAsync(EmailSendRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.SmtpHost))
                return new EmailSendResult(false, null, "Email SMTP transport requires SmtpHost.");

            // System.Net.Mail.SmtpClient only supports STARTTLS (typically port 587).
            // Port 465 (implicit SSL) causes: "Syntax error, command unrecognized".
            var port = _options.SmtpPort;
            if (port == 465)
            {
                _logger.LogWarning(
                    "Email SmtpPort 465 is not supported by SmtpClient; using 587 (STARTTLS) instead.");
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
                _logger.LogInformation("Email SMTP send succeeded To={To}", request.ToAddress);
                return new EmailSendResult(true, Guid.NewGuid().ToString("N"), null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SMTP send failed to {To}", request.ToAddress);
                return new EmailSendResult(false, null, ex.Message);
            }
        }

        private static string? TryReadRequestId(string json)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("request_id", out var id))
                    return id.GetString();
            }
            catch
            {
                // ignore parse errors
            }

            return null;
        }
    }
}
