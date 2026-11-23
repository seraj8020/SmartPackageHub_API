using HubManagement.Application.Auth.Services;
using HubManagement.Application.Intake.DTOs;
using HubManagement.Application.PackageHub.Commands;
using HubManagement.Application.PackageHub.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartPackageHub.Common.ConfigOptions;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [Route("api/v{version:apiVersion}/packages")]
    [ApiController]
    public class PackagesController : ControllerBase
    {
        readonly IMediator _mediator;
        readonly StaffSessionStore _staffSessions;

        public PackagesController(IMediator mediator, StaffSessionStore staffSessions)
        {
            _mediator = mediator;
            _staffSessions = staffSessions;
        }

        [HttpPost]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(PackageDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Create([FromBody] CreatePackageRequest request)
        {
            var result = await _mediator.Send(new CreatePackageCommand(request ?? new CreatePackageRequest())).ConfigureAwait(false);
            if (result.Conflict)
                return Conflict(new { Message = result.Message });
            if (!result.Found)
                return NotFound(new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(result.Package);
        }

        [HttpPut("{packageId:guid}/dispatch")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> DispatchPackages(Guid packageId)
        {
            var result = await _mediator.Send(new DispatchPackageCommand(packageId)).ConfigureAwait(false);
            if (!result.Found)
                return NotFound(new { succeeded = false, message = result.Message });
            if (result.Conflict)
                return Conflict(new { succeeded = false, message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { succeeded = false, message = result.Message });
            return Ok(new { succeeded = true, message = result.Message });
        }

        [HttpPost("{packageId:guid}/pickup-otp")]
        [MapToApiVersion(ApiVersions.V2)]
        [EnableRateLimiting("otp-sms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult> GeneratePickupOtp(Guid packageId, CancellationToken cancellationToken)
        {
            var auth = await GetStaffSessionAsync(cancellationToken).ConfigureAwait(false);
            if (!auth.Ok)
                return auth.Error!;

            var result = await _mediator.Send(
                new GeneratePackagePickupOtpCommand(packageId, auth.Session!.SocietyIds),
                cancellationToken).ConfigureAwait(false);
            if (!result.Found)
                return NotFound(new { Message = result.Message });
            if (result.Forbidden)
                return StatusCode(StatusCodes.Status403Forbidden, new { succeeded = false, message = result.Message });
            if (result.Conflict)
                return Conflict(new { succeeded = false, message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { succeeded = false, message = result.Message });

            return Ok(new
            {
                succeeded = true,
                pickupOtp = result.PickupOtp,
                smsQueued = result.SmsQueued,
                message = result.Message,
            });
        }

        [HttpPost("{packageId:guid}/deliver")]
        [MapToApiVersion(ApiVersions.V2)]
        [EnableRateLimiting("otp-sms")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> DeliverPackages([FromRoute] Guid packageId, [FromBody] DeliverPackageRequest request)
        {
            var auth = await GetStaffSessionAsync(HttpContext.RequestAborted).ConfigureAwait(false);
            if (!auth.Ok)
                return auth.Error!;

            var result = await _mediator.Send(
                new DeliverPackageCommand(packageId, request, auth.Session!.SocietyIds),
                HttpContext.RequestAborted).ConfigureAwait(false);
            if (!result.Found)
                return NotFound(new { Message = result.Message });
            if (result.Forbidden)
                return StatusCode(StatusCodes.Status403Forbidden, new { succeeded = false, message = result.Message });
            if (result.Conflict)
                return Conflict(new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { succeeded = false, message = result.Message });

            return Ok(new { succeeded = true, message = result.Message });
        }

        [HttpPost("{packageId:guid}/cod/approve")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> ApproveCod(Guid packageId)
        {
            var result = await _mediator.Send(new RespondCodCommand(packageId, Approve: true)).ConfigureAwait(false);
            return MapCodResult(result);
        }

        [HttpPost("{packageId:guid}/cod/reject")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> RejectCod(Guid packageId)
        {
            var result = await _mediator.Send(new RespondCodCommand(packageId, Approve: false)).ConfigureAwait(false);
            return MapCodResult(result);
        }

        [HttpPost("{packageId:guid}/shelf")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ConfirmIntakeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> AssignShelf(Guid packageId, [FromBody] AssignShelfRequest? request)
        {
            var result = await _mediator.Send(new AssignShelfCommand(
                packageId,
                request?.ShelfCode,
                request?.IsColdStorage ?? false)).ConfigureAwait(false);

            if (!result.Found)
                return NotFound(new { Message = result.Message });
            if (result.Conflict)
                return Conflict(new { succeeded = false, message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { succeeded = false, message = result.Message });

            return Ok(result.Receipt);
        }

        /// <summary>Internal multi-hop: move package between zones (Hub → tower cage, etc.).</summary>
        [HttpPost("{packageId:guid}/custody-transfer")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(PackageDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> TransferCustodyZone(
            Guid packageId,
            [FromBody] TransferPackageZoneRequest? request,
            CancellationToken cancellationToken)
        {
            var auth = await GetStaffSessionAsync(cancellationToken).ConfigureAwait(false);
            if (!auth.Ok)
                return auth.Error!;

            var result = await _mediator.Send(
                new TransferPackageZoneCommand(
                    packageId,
                    request ?? new TransferPackageZoneRequest(),
                    auth.Session!.UserId,
                    auth.Session.SocietyIds),
                cancellationToken).ConfigureAwait(false);

            if (!result.Found)
                return NotFound(new { Message = result.Message });
            if (result.Forbidden)
                return StatusCode(StatusCodes.Status403Forbidden, new { succeeded = false, message = result.Message });
            if (result.Conflict)
                return Conflict(new { succeeded = false, message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { succeeded = false, message = result.Message });

            return Ok(new { succeeded = true, message = result.Message, package = result.Package });
        }

        private async Task<(bool Ok, StaffSession? Session, ActionResult? Error)> GetStaffSessionAsync(
            CancellationToken cancellationToken)
        {
            var authorization = Request.Headers.Authorization.ToString();
            var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authorization["Bearer ".Length..].Trim()
                : null;
            if (string.IsNullOrWhiteSpace(token))
                return (false, null, Unauthorized(new { succeeded = false, message = "Staff sign-in required." }));

            var session = await _staffSessions.GetSessionAsync(token, cancellationToken).ConfigureAwait(false);
            if (session is null)
                return (false, null, Unauthorized(new { succeeded = false, message = "Staff sign-in required." }));
            if (!string.Equals(session.Role, "Staff", StringComparison.OrdinalIgnoreCase))
                return (false, null, StatusCode(StatusCodes.Status403Forbidden, new { succeeded = false, message = "Staff access required." }));

            return (true, session, null);
        }

        private static ActionResult MapCodResult(CodResponseResult result)
        {
            if (!result.Found)
                return new NotFoundObjectResult(new { Message = result.Message });
            if (result.Conflict)
            {
                return new ConflictObjectResult(new
                {
                    succeeded = false,
                    message = result.Message,
                    status = result.Status,
                    codApprovalStatus = result.CodApprovalStatus,
                });
            }

            if (!result.Succeeded)
                return new BadRequestObjectResult(new { succeeded = false, message = result.Message });

            return new OkObjectResult(new
            {
                succeeded = true,
                message = result.Message,
                pickupOtp = result.PickupOtp,
                status = result.Status,
                codApprovalStatus = result.CodApprovalStatus,
            });
        }
    }
}
