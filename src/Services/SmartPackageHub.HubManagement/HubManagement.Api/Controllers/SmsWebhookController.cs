using System.Text.Json.Serialization;
using HubManagement.Core.Hubs.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Types;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    /// <summary>
    /// Fast2SMS delivery receipts. Configure this URL in the Fast2SMS webhook panel.
    /// Delivery is not assumed from HTTP 200 on send — only webhook (or DLR poll) marks Delivered/Failed.
    /// </summary>
    [Route("api/v{version:apiVersion}/webhooks/fast2sms")]
    [ApiController]
    [EnableRateLimiting("sms-webhook")]
    public sealed class SmsWebhookController : ControllerBase
    {
        private readonly IRepository<Notification> _notifications;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SmsWebhookController> _logger;

        public SmsWebhookController(
            IRepository<Notification> notifications,
            IUnitOfWork unitOfWork,
            ILogger<SmsWebhookController> logger)
        {
            _notifications = notifications;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// match by request_id / udf2, mark Delivered or Failed
        /// </summary>
        /// <param name="body"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Receive([FromBody] Fast2SmsWebhookPayload? body, CancellationToken cancellationToken)
        {
            var payload = body ?? await TryBindFormAsync(cancellationToken).ConfigureAwait(false);
            if (payload is null || string.IsNullOrWhiteSpace(payload.RequestId))
                return Ok(new { received = false });

            var notification = await _notifications.FirstOrDefaultAsync(
                n => n.ProviderRequestId == payload.RequestId).ConfigureAwait(false);

            // Also match via udf2 (notification id) when request_id mapping is delayed.
            if (notification is null && Guid.TryParse(payload.Udf2, out var notificationId))
                notification = await _notifications.GetByIdAsync(notificationId).ConfigureAwait(false);

            if (notification is null)
            {
                _logger.LogInformation(
                    "Fast2SMS webhook for unknown request_id={RequestId} status={Status}",
                    payload.RequestId,
                    payload.Status);
                return Ok(new { received = true, matched = false });
            }

            var status = payload.Status?.Trim() ?? string.Empty;
            if (status.Equals("delivered", StringComparison.OrdinalIgnoreCase)
                || status.Equals("success", StringComparison.OrdinalIgnoreCase))
            {
                if (notification.DeliveredAt is null)
                {
                    notification.MarkAsDelivered();
                    await _unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else if (status.Equals("failed", StringComparison.OrdinalIgnoreCase)
                     || status.Equals("undelivered", StringComparison.OrdinalIgnoreCase)
                     || status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            {
                var reason = payload.FailureReason
                    ?? payload.StatusDescription
                    ?? payload.Description
                    ?? "Delivery failed";
                notification.MarkAsFailed(reason);
                await _unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            return Ok(new { received = true, matched = true, notificationId = notification.Id });
        }

        /// <summary>
        /// also accept form-urlencoded webhooks
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Fast2SmsWebhookPayload?> TryBindFormAsync(CancellationToken cancellationToken)
        {
            if (!Request.HasFormContentType)
                return null;

            var form = await Request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
            return new Fast2SmsWebhookPayload
            {
                RequestId = form["request_id"],
                Status = form["status"],
                Mobile = form["mobile"],
                FailureReason = form["failure_reason"],
                StatusDescription = form["status_description"],
                Description = form["description"],
                Udf1 = form["udf1"],
                Udf2 = form["udf2"],
                Udf3 = form["udf3"]
            };
        }
    }

    public sealed class Fast2SmsWebhookPayload
    {
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("failure_reason")]
        public string? FailureReason { get; set; }

        [JsonPropertyName("status_description")]
        public string? StatusDescription { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("udf1")]
        public string? Udf1 { get; set; }

        [JsonPropertyName("udf2")]
        public string? Udf2 { get; set; }

        [JsonPropertyName("udf3")]
        public string? Udf3 { get; set; }
    }
}
