using HubManagement.Application.Auth.Services;
using HubManagement.Application.Returns.Commands;
using HubManagement.Application.Returns.DTOs;
using HubManagement.Application.Returns.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.ConfigOptions;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiController]
    public class ReturnsController : ControllerBase
    {
        readonly IMediator _mediator;
        readonly StaffSessionStore _staffSessions;

        public ReturnsController(IMediator mediator, StaffSessionStore staffSessions)
        {
            _mediator = mediator;
            _staffSessions = staffSessions;
        }

        [HttpPost("packages/{packageId:guid}/returns")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(PackageReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> RequestReturn(
            Guid packageId,
            [FromBody] RequestPackageReturnRequest? request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new RequestPackageReturnCommand(packageId, request ?? new RequestPackageReturnRequest()),
                cancellationToken).ConfigureAwait(false);
            return MapCommandResult(result);
        }

        [HttpPost("returns/{returnId:guid}/photos")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(PackageReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> UploadPhoto(
            Guid returnId,
            [FromBody] UploadPackageReturnPhotoRequest? request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UploadPackageReturnPhotoCommand(returnId, request ?? new UploadPackageReturnPhotoRequest()),
                cancellationToken).ConfigureAwait(false);
            return MapCommandResult(result);
        }

        [HttpGet("societies/{societyId:guid}/returns")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(IReadOnlyList<PackageReturnDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> ListSocietyReturns(
            Guid societyId,
            [FromQuery] bool openOnly = true,
            CancellationToken cancellationToken = default)
        {
            var staffAuth = await TryGetStaffSessionAsync(cancellationToken).ConfigureAwait(false);
            if (!staffAuth.Ok)
                return staffAuth.Error!;
            var session = staffAuth.Session;
            if (!session!.SocietyIds.Contains(societyId))
                return StatusCode(StatusCodes.Status403Forbidden, new { succeeded = false, message = "Staff is not assigned to this society." });

            var items = await _mediator.Send(
                new ListSocietyPackageReturnsQuery(societyId, openOnly),
                cancellationToken).ConfigureAwait(false);
            return Ok(items);
        }

        [HttpGet("returns/{returnId:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(PackageReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetById(Guid returnId, CancellationToken cancellationToken)
        {
            var dto = await _mediator.Send(new GetPackageReturnByIdQuery(returnId), cancellationToken)
                .ConfigureAwait(false);
            if (dto is null)
                return NotFound(new { Message = "Return was not found." });
            return Ok(dto);
        }

        [HttpGet("packages/{packageId:guid}/return")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(PackageReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetByPackageId(Guid packageId, CancellationToken cancellationToken)
        {
            var dto = await _mediator.Send(new GetPackageReturnByPackageIdQuery(packageId), cancellationToken)
                .ConfigureAwait(false);
            if (dto is null)
                return NotFound(new { Message = "No return found for this package." });
            return Ok(dto);
        }

        [HttpPost("returns/{returnId:guid}/receive")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(PackageReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> MarkReceived(
            Guid returnId,
            [FromBody] MarkPackageReturnReceivedRequest? request,
            CancellationToken cancellationToken)
        {
            var staffAuth = await TryGetStaffSessionAsync(cancellationToken).ConfigureAwait(false);
            if (!staffAuth.Ok)
                return staffAuth.Error!;
            var session = staffAuth.Session;

            var result = await _mediator.Send(
                new MarkPackageReturnReceivedCommand(returnId, session!.UserId, session.SocietyIds, request),
                cancellationToken).ConfigureAwait(false);
            return MapCommandResult(result);
        }

        [HttpPost("returns/{returnId:guid}/shelf")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(PackageReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> AssignShelf(
            Guid returnId,
            [FromBody] AssignPackageReturnShelfRequest? request,
            CancellationToken cancellationToken)
        {
            var staffAuth = await TryGetStaffSessionAsync(cancellationToken).ConfigureAwait(false);
            if (!staffAuth.Ok)
                return staffAuth.Error!;
            var session = staffAuth.Session;

            var result = await _mediator.Send(
                new AssignPackageReturnShelfCommand(returnId, session!.UserId, session.SocietyIds, request),
                cancellationToken).ConfigureAwait(false);
            return MapCommandResult(result);
        }

        [HttpPost("returns/{returnId:guid}/courier-pickup")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(PackageReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> MarkCourierPickup(
            Guid returnId,
            [FromBody] MarkPackageReturnCourierPickupRequest? request,
            CancellationToken cancellationToken)
        {
            var staffAuth = await TryGetStaffSessionAsync(cancellationToken).ConfigureAwait(false);
            if (!staffAuth.Ok)
                return staffAuth.Error!;
            var session = staffAuth.Session;

            var result = await _mediator.Send(
                new MarkPackageReturnCourierPickupCommand(returnId, session!.UserId, session.SocietyIds, request),
                cancellationToken).ConfigureAwait(false);
            return MapCommandResult(result);
        }

        private async Task<(bool Ok, StaffSession? Session, ActionResult? Error)> TryGetStaffSessionAsync(
            CancellationToken cancellationToken)
        {
            var authorization = Request.Headers.Authorization.ToString();
            var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authorization["Bearer ".Length..].Trim()
                : null;
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, null, Unauthorized(new { succeeded = false, message = "Staff sign-in required." }));
            }

            var session = await _staffSessions.GetSessionAsync(token, cancellationToken).ConfigureAwait(false);
            if (session is null)
            {
                return (false, null, Unauthorized(new { succeeded = false, message = "Staff sign-in required." }));
            }

            if (!string.Equals(session.Role, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, StatusCode(StatusCodes.Status403Forbidden, new { succeeded = false, message = "Staff access required." }));
            }

            return (true, session, null);
        }

        private static ActionResult MapCommandResult(PackageReturnCommandResult result)
        {
            if (result.Forbidden)
                return new ObjectResult(new { succeeded = false, message = result.Message })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
            if (!result.Found)
                return new NotFoundObjectResult(new { Message = result.Message });
            if (result.Conflict)
                return new ConflictObjectResult(new { succeeded = false, message = result.Message });
            if (!result.Succeeded)
                return new BadRequestObjectResult(new { succeeded = false, message = result.Message });

            return new OkObjectResult(result.Return);
        }
    }
}
