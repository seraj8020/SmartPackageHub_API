using HubManagement.Application.Support.Commands;
using HubManagement.Application.Support.DTOs;
using HubManagement.Application.Support.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartPackageHub.Common.ConfigOptions;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    /// <summary>Resident in-app Support (Traizr-style ticket list + new request).</summary>
    [Route("api/v{version:apiVersion}/support")]
    [ApiController]
    public sealed class SupportController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SupportController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Configurable support inbox (default hello@byonverse.com).</summary>
        [HttpGet("config")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ResidentSupportConfigDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ResidentSupportConfigDto>> GetConfig(CancellationToken cancellationToken)
        {
            var config = await _mediator.Send(new GetResidentSupportConfigQuery(), cancellationToken)
                .ConfigureAwait(false);
            return Ok(config);
        }

        /// <summary>List support tickets for a resident (newest first).</summary>
        [HttpGet("requests")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<SupportRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ListRequests(
            [FromQuery] Guid residentId,
            CancellationToken cancellationToken)
        {
            if (residentId == Guid.Empty)
                return BadRequest(new { message = "residentId is required." });

            var rows = await _mediator
                .Send(new GetResidentSupportRequestsQuery(residentId), cancellationToken)
                .ConfigureAwait(false);
            if (rows is null)
                return NotFound(new { message = "Resident was not found." });

            return Ok(rows);
        }

        /// <summary>Create a support request with resident/society context attached.</summary>
        [HttpPost("requests")]
        [MapToApiVersion(ApiVersions.V2)]
        [EnableRateLimiting("otp-sms")]
        [ProducesResponseType(typeof(SupportRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> CreateRequest(
            [FromBody] CreateSupportRequestDto? request,
            CancellationToken cancellationToken)
        {
            if (request is null)
                return BadRequest(new { message = "Support request body is required." });
            if (request.ResidentId == Guid.Empty && string.IsNullOrWhiteSpace(request.ResidentPhone))
                return BadRequest(new { message = "Resident id or phone is required." });

            var result = await _mediator
                .Send(new CreateSupportRequestCommand(request), cancellationToken)
                .ConfigureAwait(false);

            if (!result.Found)
                return NotFound(new { message = result.Message });
            if (!result.Succeeded || result.Request is null)
                return BadRequest(new { message = result.Message });

            return Ok(result.Request);
        }
    }
}
