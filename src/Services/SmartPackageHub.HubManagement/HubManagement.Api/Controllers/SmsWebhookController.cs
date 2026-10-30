using HubManagement.Application.Messages.Commands;
using HubManagement.Application.Messages.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartPackageHub.Common.ConfigOptions;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    /// <summary>
    /// Fast2SMS delivery receipts. Configure this URL in the Fast2SMS webhook panel.
    /// </summary>
    [Route("api/v{version:apiVersion}/webhooks/fast2sms")]
    [ApiController]
    [EnableRateLimiting("sms-webhook")]
    public sealed class SmsWebhookController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SmsWebhookController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Accepts JSON or form-urlencoded Fast2SMS DLRs and forwards to the application layer.
        /// </summary>
        [HttpPost]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Receive(
            [FromBody] Fast2SmsWebhookPayload? body,
            CancellationToken cancellationToken)
        {
            var payload = body ?? await TryBindFormAsync(cancellationToken).ConfigureAwait(false);
            var result = await _mediator
                .Send(new ProcessFast2SmsWebhookCommand(payload), cancellationToken)
                .ConfigureAwait(false);

            if (!result.Received)
                return Ok(new { received = false });

            if (!result.Matched)
                return Ok(new { received = true, matched = false });

            return Ok(new { received = true, matched = true, notificationId = result.NotificationId });
        }

        /// <summary>Maps form-urlencoded webhook posts into the shared payload DTO.</summary>
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
}
