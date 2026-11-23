using HubManagement.Application.Auth.Services;
using HubManagement.Application.Courier.Commands;
using HubManagement.Application.Courier.Queries;
using HubManagement.Application.Couriers.DTOs;
using HubManagement.Application.Intake.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.ConfigOptions;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/couriers")]
    public sealed class CouriersController : ControllerBase
    {
        private readonly PlatformAdminSessionStore _sessions;
        private readonly IMediator _mediator;

        public CouriersController(PlatformAdminSessionStore sessions, IMediator mediator)
        {
            _sessions = sessions;
            _mediator = mediator;
        }

        /// <summary>
        /// Distinct carrier names (alias of GET /intake/carriers for older clients).
        /// </summary>
        [HttpGet("carriers")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetCarriers([FromQuery] Guid? societyId, CancellationToken cancellationToken)
        {
            var carriers = await _mediator
                .Send(new GetIntakeCarriersQuery(societyId), cancellationToken)
                .ConfigureAwait(false);
            return Ok(carriers);
        }

        [HttpGet]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<CourierAgentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> List([FromQuery] Guid? societyId, CancellationToken cancellationToken)
        {
            if (!TryGetAdminSession(out var session, out var authError)) return authError!;

            var result = await _mediator.Send(
                new ListCourierAgentsQuery(session!.Role, session.SocietyIds, societyId),
                cancellationToken).ConfigureAwait(false);
            return MapListResult(result);
        }

        [HttpPost]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(CourierAgentDto), StatusCodes.Status201Created)]
        public async Task<ActionResult> Create(
            [FromBody] CreateCourierAgentRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetAdminSession(out var session, out var authError)) return authError!;
            ArgumentNullException.ThrowIfNull(request);

            var result = await _mediator.Send(
                new CreateCourierAgentCommand(session!.Role, session.SocietyIds, request),
                cancellationToken).ConfigureAwait(false);
            return MapAgentCommandResult(result, StatusCodes.Status201Created);
        }

        [HttpPut("{id:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(CourierAgentDto), StatusCodes.Status200OK)]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] UpdateCourierAgentRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetAdminSession(out var session, out var authError)) return authError!;
            ArgumentNullException.ThrowIfNull(request);

            var result = await _mediator.Send(
                new UpdateCourierAgentCommand(id, session!.Role, session.SocietyIds, request),
                cancellationToken).ConfigureAwait(false);
            return MapAgentCommandResult(result, StatusCodes.Status200OK);
        }

        [HttpPost("{id:guid}/blacklist")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(CourierBlacklistDto), StatusCodes.Status200OK)]
        public async Task<ActionResult> Blacklist(
            Guid id,
            [FromBody] BlacklistCourierRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetAdminSession(out var session, out var authError)) return authError!;
            ArgumentNullException.ThrowIfNull(request);

            var result = await _mediator.Send(
                new BlacklistCourierAgentCommand(id, session!.Role, session.SocietyIds, request),
                cancellationToken).ConfigureAwait(false);
            return MapBlacklistCommandResult(result);
        }

        [HttpGet("blacklist")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<CourierBlacklistDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> ListBlacklist(
            [FromQuery] Guid? societyId,
            CancellationToken cancellationToken)
        {
            if (!TryGetAdminSession(out var session, out var authError)) return authError!;

            var result = await _mediator.Send(
                new ListCourierBlacklistQuery(session!.Role, session.SocietyIds, societyId),
                cancellationToken).ConfigureAwait(false);
            return MapListResult(result);
        }

        [HttpPost("blacklist/{id:guid}/lift")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> LiftBlacklist(Guid id, CancellationToken cancellationToken)
        {
            if (!TryGetAdminSession(out var session, out var authError)) return authError!;

            var result = await _mediator.Send(
                new LiftCourierBlacklistCommand(id, session!.Role, session.SocietyIds),
                cancellationToken).ConfigureAwait(false);
            return MapMessageCommandResult(result);
        }

        private bool TryGetAdminSession(out PlatformAdminSession? session, out ActionResult? error)
        {
            session = null;
            error = null;
            var auth = Request.Headers.Authorization.ToString();
            var token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? auth["Bearer ".Length..].Trim()
                : null;
            if (string.IsNullOrWhiteSpace(token) || !_sessions.TryGetSession(token, out session))
            {
                error = Unauthorized(new { Message = "Admin sign-in required." });
                return false;
            }

            if (!string.Equals(session!.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(session.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                error = StatusCode(StatusCodes.Status403Forbidden, new { Message = "Admin access required." });
                return false;
            }

            return true;
        }

        private static ActionResult MapListResult<T>(CourierListResult<T> result)
        {
            if (result.Forbidden)
                return new ObjectResult(new { Message = result.Message })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
            return new OkObjectResult(result.Items);
        }

        private static ActionResult MapAgentCommandResult(CourierCommandResult result, int successStatusCode)
        {
            if (result.Forbidden)
                return new ObjectResult(new { Message = result.Message })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
            if (!result.Found)
                return new NotFoundObjectResult(new { Message = result.Message });
            if (result.Conflict)
                return new ConflictObjectResult(new { Message = result.Message });
            if (!result.Succeeded)
                return new BadRequestObjectResult(new { Message = result.Message });

            return new ObjectResult(result.Agent) { StatusCode = successStatusCode };
        }

        private static ActionResult MapBlacklistCommandResult(CourierCommandResult result)
        {
            if (result.Forbidden)
                return new ObjectResult(new { Message = result.Message })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
            if (!result.Found)
                return new NotFoundObjectResult(new { Message = result.Message });
            if (result.Conflict)
                return new ConflictObjectResult(new { Message = result.Message });
            if (!result.Succeeded)
                return new BadRequestObjectResult(new { Message = result.Message });

            return new OkObjectResult(result.Blacklist);
        }

        private static ActionResult MapMessageCommandResult(CourierCommandResult result)
        {
            if (result.Forbidden)
                return new ObjectResult(new { Message = result.Message })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
            if (!result.Found)
                return new NotFoundObjectResult(new { Message = result.Message });
            if (!result.Succeeded)
                return new BadRequestObjectResult(new { Message = result.Message });

            return new OkObjectResult(new { Message = result.Message });
        }
    }
}