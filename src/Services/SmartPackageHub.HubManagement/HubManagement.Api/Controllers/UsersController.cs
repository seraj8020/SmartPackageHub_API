using HubManagement.Application.Auth.Services;
using HubManagement.Application.HubUsers.Commands;
using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Application.HubUsers.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.ConfigOptions;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/users")]
    public class UsersController : ControllerBase
    {
        readonly IMediator _mediator;
        readonly PlatformAdminSessionStore _sessions;

        public UsersController(IMediator mediator, PlatformAdminSessionStore sessions)
        {
            _mediator = mediator;
            _sessions = sessions;
        }

        [HttpPost("invite")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Invite([FromBody] InviteUserRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetSession(out var session, out var authError)) return authError!;
            ArgumentNullException.ThrowIfNull(request);

            var result = await _mediator.Send(new InviteUserCommand(
                session!.Role,
                session.SocietyIds,
                request.Username,
                request.Email,
                request.PhoneNumber,
                request.Role,
                request.Password,
                request.SocietyId), cancellationToken).ConfigureAwait(false);

            return MapUserCommandResult(result, created: true);
        }

        [HttpGet("availability")]
        [MapToApiVersion(ApiVersions.V2)]
        public async Task<ActionResult> CheckAvailability(
            [FromQuery] string field,
            [FromQuery] string value,
            [FromQuery] Guid? excludeUserId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(field);
            ArgumentNullException.ThrowIfNull(value);
            if (!TryGetSession(out var session, out var authError)) return authError!;

            var result = await _mediator.Send(new CheckUserAvailabilityQuery(
                session!.Role,
                field,
                value,
                excludeUserId), cancellationToken).ConfigureAwait(false);

            if (result.Forbidden)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Available = result.Available });
        }

        [HttpGet]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<UserDetailsDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAll([FromQuery] bool residentsOnly = false, CancellationToken cancellationToken = default)
        {
            if (!TryGetSession(out var session, out var authError)) return authError!;

            var result = await _mediator.Send(new GetAdminUsersQuery(
                session!.Role,
                session.SocietyIds,
                residentsOnly), cancellationToken).ConfigureAwait(false);

            if (result.Forbidden)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = result.Message });

            return Ok(result.Users);
        }

        [HttpPut("residents/{id:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> UpdateResident(Guid id, [FromBody] UpdateResidentRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetSession(out var session, out var authError)) return authError!;
            ArgumentNullException.ThrowIfNull(request);

            var result = await _mediator.Send(new UpdateResidentUserCommand(
                id,
                session!.Role,
                session.SocietyIds,
                request.Username,
                request.PhoneNumber,
                request.TowerWing,
                request.UnitNumber,
                request.Status,
                request.SocietyId), cancellationToken).ConfigureAwait(false);

            return MapUserCommandResult(result);
        }

        [HttpGet("{id:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Get(Guid id)
        {
            if (!TryGetSession(out var session, out var authError)) return authError!;
            var isPlatformAdmin = string.Equals(session!.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            var isSocietyAdmin = string.Equals(session.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            if (!isPlatformAdmin && !isSocietyAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Admin access required." });

            var user = await _mediator.Send(new GetUserByIdQuery(id)).ConfigureAwait(false);
            if (user == null)
                return NotFound();

            if (isSocietyAdmin && !user.SocietyIds.Any(session.SocietyIds.Contains))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You cannot view users outside your society." });

            return Ok(user);
        }

        [HttpPost]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetSession(out var session, out var authError)) return authError!;
            if (!string.Equals(session!.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Platform admin access required. Use the invite endpoint to create society users." });
            ArgumentNullException.ThrowIfNull(request);
            if (!request.SocietyId.HasValue || request.SocietyId.Value == Guid.Empty)
                return BadRequest(new { Field = "Society", Message = "Select a society." });

            var result = await _mediator.Send(new CreateUserCommand(
                request.Username,
                request.Email,
                request.Role,
                request.IsAdmin,
                request.Status,
                request.Password,
                request.PhoneNumber,
                SocietyId: request.SocietyId), cancellationToken).ConfigureAwait(false);

            return MapUserCommandResult(result);
        }

        [HttpPut("{id:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetSession(out var session, out var authError)) return authError!;
            ArgumentNullException.ThrowIfNull(request);

            var result = await _mediator.Send(new UpdateUserCommand(
                id,
                request.Username,
                request.Email,
                request.Role,
                request.IsAdmin,
                request.Status,
                request.PhoneNumber,
                request.SocietyIds,
                request.SocietyId,
                session!.Role,
                session.SocietyIds), cancellationToken).ConfigureAwait(false);

            return MapUserCommandResult(result);
        }

        private static ActionResult MapUserCommandResult(UserCommandResult result, bool created = false)
        {
            if (result.Forbidden)
                return new ObjectResult(new { Message = result.Message }) { StatusCode = StatusCodes.Status403Forbidden };
            if (result.Conflict)
            {
                return result.ConflictField is not null
                    ? new ConflictObjectResult(new { Field = result.ConflictField, Message = result.Message })
                    : new ConflictObjectResult(new { Message = result.Message });
            }
            if (!result.Found)
                return new NotFoundResult();
            if (!result.Succeeded)
                return new BadRequestObjectResult(new { Message = result.Message });
            return created
                ? new ObjectResult(result.User) { StatusCode = StatusCodes.Status201Created }
                : new OkObjectResult(result.User);
        }

        private bool TryGetSession(out PlatformAdminSession? session, out ActionResult? error)
        {
            session = null;
            error = null;
            var auth = Request.Headers.Authorization.ToString();
            var token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? auth["Bearer ".Length..].Trim() : null;
            if (string.IsNullOrWhiteSpace(token) || !_sessions.TryGetSession(token, out session))
            {
                error = Unauthorized(new { Message = "Admin sign-in required." });
                return false;
            }
            return true;
        }
    }
}