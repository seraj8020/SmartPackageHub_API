using HubManagement.Application.Auth.Services;
using HubManagement.Application.Couriers.DTOs;
using HubManagement.Application.Intake.Queries;
using HubManagement.Core.Hubs.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPackageHub.Common.ConfigOptions;
using SmartPackageHub.Common.Services.Messaging.Guards;
using SmartPackageHub.HubManagement.Infrastructure;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/couriers")]
    public sealed class CouriersController : ControllerBase
    {
        private readonly HubDbContext _db;
        private readonly PlatformAdminSessionStore _sessions;
        private readonly IMediator _mediator;

        public CouriersController(HubDbContext db, PlatformAdminSessionStore sessions, IMediator mediator)
        {
            _db = db;
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
            if (!TryResolveSocietyFilter(session!, societyId, out var filterSocietyId, out var societyError))
                return societyError!;

            var agentsQuery = _db.CourierAgents.AsNoTracking().AsQueryable();
            if (filterSocietyId.HasValue)
                agentsQuery = agentsQuery.Where(agent => agent.SocietyId == filterSocietyId.Value);

            var agents = await agentsQuery
                .OrderBy(agent => agent.CarrierName)
                .ThenBy(agent => agent.AgentName)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var societyIds = agents.Select(agent => agent.SocietyId).Distinct().ToList();
            var societies = await _db.Societies.AsNoTracking()
                .Where(society => societyIds.Contains(society.Id))
                .ToDictionaryAsync(society => society.Id, society => society.Name, cancellationToken)
                .ConfigureAwait(false);

            var blacklists = await _db.CourierBlacklists.AsNoTracking()
                .Where(item => item.IsActive && (!filterSocietyId.HasValue || item.SocietyId == filterSocietyId.Value))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var rows = agents.Select(agent =>
            {
                var hit = string.IsNullOrWhiteSpace(agent.Phone)
                    ? null
                    : blacklists.FirstOrDefault(item =>
                        item.SocietyId == agent.SocietyId &&
                        PhonesMatch(item.Phone, agent.Phone));
                return ToDto(agent, societies.GetValueOrDefault(agent.SocietyId) ?? string.Empty, hit);
            }).ToList();

            return Ok(rows);
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
            if (!TryResolveSocietyWrite(session!, request.SocietyId, out var societyId, out var societyError))
                return societyError!;

            var validationError = await ValidateCreateAsync(request, societyId, cancellationToken).ConfigureAwait(false);
            if (validationError is not null) return validationError;

            string? phone = null;
            if (!string.IsNullOrWhiteSpace(request.Phone) &&
                !PhoneNumberGuard.TryNormalizeIndiaMobile(request.Phone, out phone))
            {
                return BadRequest(new { Message = "Enter a valid 10-digit Indian mobile number." });
            }

            try
            {
                var agent = CourierAgent.Create(
                    societyId,
                    request.CarrierName!,
                    request.AgentName,
                    phone,
                    request.VehicleNumber,
                    request.EmployeeOrBadgeNumber);
                _db.CourierAgents.Add(agent);
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                var societyName = await _db.Societies.AsNoTracking()
                    .Where(society => society.Id == societyId)
                    .Select(society => society.Name)
                    .FirstAsync(cancellationToken)
                    .ConfigureAwait(false);
                return StatusCode(StatusCodes.Status201Created, ToDto(agent, societyName, null));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
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

            var agent = await _db.CourierAgents.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                .ConfigureAwait(false);
            if (agent is null) return NotFound(new { Message = "Courier was not found." });
            if (!CanAccessSociety(session!, agent.SocietyId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You cannot manage couriers outside your society." });

            if (string.IsNullOrWhiteSpace(request.CarrierName))
                return BadRequest(new { Message = "Courier name is required." });

            string? phone = null;
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.Phone, out phone))
                    return BadRequest(new { Message = "Enter a valid 10-digit Indian mobile number." });

                var phoneTaken = await _db.CourierAgents.AsNoTracking()
                    .AnyAsync(
                        item => item.SocietyId == agent.SocietyId && item.Id != id && item.Phone == phone,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (phoneTaken)
                    return Conflict(new { Message = "Another courier already uses this mobile number in the society." });
            }

            try
            {
                agent.UpdateDetails(
                    request.CarrierName,
                    request.AgentName,
                    phone,
                    request.VehicleNumber,
                    request.EmployeeOrBadgeNumber);
                if (request.IsActive == true) agent.Activate();
                if (request.IsActive == false) agent.Deactivate();
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                var societyName = await _db.Societies.AsNoTracking()
                    .Where(society => society.Id == agent.SocietyId)
                    .Select(society => society.Name)
                    .FirstAsync(cancellationToken)
                    .ConfigureAwait(false);
                var hit = await _db.CourierBlacklists.AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.IsActive && item.SocietyId == agent.SocietyId && item.Phone == agent.Phone,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Ok(ToDto(agent, societyName, hit));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
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

            var agent = await _db.CourierAgents.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                .ConfigureAwait(false);
            if (agent is null) return NotFound(new { Message = "Courier was not found." });
            if (!CanAccessSociety(session!, agent.SocietyId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You cannot manage couriers outside your society." });

            if (string.IsNullOrWhiteSpace(agent.Phone))
                return BadRequest(new { Message = "Add a mobile number before blacklisting this courier." });

            var reason = string.IsNullOrWhiteSpace(request.Reason)
                ? "Blacklisted by admin"
                : request.Reason.Trim();

            var existing = await _db.CourierBlacklists
                .FirstOrDefaultAsync(
                    item => item.IsActive && item.SocietyId == agent.SocietyId && item.Phone == agent.Phone,
                    cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
                return Conflict(new { Message = "This courier is already blacklisted." });

            var entry = CourierBlacklist.Create(
                agent.SocietyId,
                agent.CarrierName,
                agent.AgentName,
                agent.Phone,
                reason);
            agent.Deactivate();
            _db.CourierBlacklists.Add(entry);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var societyName = await _db.Societies.AsNoTracking()
                .Where(society => society.Id == agent.SocietyId)
                .Select(society => society.Name)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return Ok(new CourierBlacklistDto
            {
                Id = entry.Id,
                SocietyId = entry.SocietyId,
                SocietyName = societyName,
                CarrierName = entry.CarrierName,
                AgentName = entry.AgentName,
                Phone = entry.Phone,
                Reason = entry.Reason,
                BlacklistedAt = entry.BlacklistedAt,
                IsActive = entry.IsActive,
            });
        }

        [HttpGet("blacklist")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<CourierBlacklistDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> ListBlacklist(
            [FromQuery] Guid? societyId,
            CancellationToken cancellationToken)
        {
            if (!TryGetAdminSession(out var session, out var authError)) return authError!;
            if (!TryResolveSocietyFilter(session!, societyId, out var filterSocietyId, out var societyError))
                return societyError!;

            var query = _db.CourierBlacklists.AsNoTracking().Where(item => item.IsActive);
            if (filterSocietyId.HasValue)
                query = query.Where(item => item.SocietyId == filterSocietyId.Value);

            var rows = await query
                .OrderByDescending(item => item.BlacklistedAt)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var societyIds = rows.Select(item => item.SocietyId).Distinct().ToList();
            var societies = await _db.Societies.AsNoTracking()
                .Where(society => societyIds.Contains(society.Id))
                .ToDictionaryAsync(society => society.Id, society => society.Name, cancellationToken)
                .ConfigureAwait(false);

            return Ok(rows.Select(item => new CourierBlacklistDto
            {
                Id = item.Id,
                SocietyId = item.SocietyId,
                SocietyName = societies.GetValueOrDefault(item.SocietyId) ?? string.Empty,
                CarrierName = item.CarrierName,
                AgentName = item.AgentName,
                Phone = item.Phone,
                Reason = item.Reason,
                BlacklistedAt = item.BlacklistedAt,
                IsActive = item.IsActive,
            }).ToList());
        }

        [HttpPost("blacklist/{id:guid}/lift")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> LiftBlacklist(Guid id, CancellationToken cancellationToken)
        {
            if (!TryGetAdminSession(out var session, out var authError)) return authError!;

            var entry = await _db.CourierBlacklists.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                .ConfigureAwait(false);
            if (entry is null) return NotFound(new { Message = "Blacklist entry was not found." });
            if (!CanAccessSociety(session!, entry.SocietyId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You cannot manage couriers outside your society." });

            entry.Deactivate();

            var agent = await _db.CourierAgents
                .FirstOrDefaultAsync(
                    item => item.SocietyId == entry.SocietyId && item.Phone == entry.Phone,
                    cancellationToken)
                .ConfigureAwait(false);
            agent?.Activate();

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Ok(new { Message = "Blacklist lifted." });
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

        private static bool CanAccessSociety(PlatformAdminSession session, Guid societyId)
        {
            if (string.Equals(session.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase))
                return true;
            return session.SocietyIds.Contains(societyId);
        }

        private static bool TryResolveSocietyFilter(
            PlatformAdminSession session,
            Guid? requestedSocietyId,
            out Guid? societyId,
            out ActionResult? error)
        {
            societyId = null;
            error = null;
            var isPlatform = string.Equals(session.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            if (isPlatform)
            {
                societyId = requestedSocietyId is Guid id && id != Guid.Empty ? id : null;
                return true;
            }

            if (session.SocietyIds.Count == 0)
            {
                error = ForbiddenResult("No society is linked to this admin.");
                return false;
            }

            if (requestedSocietyId is Guid asked && asked != Guid.Empty && !session.SocietyIds.Contains(asked))
            {
                error = ForbiddenResult("You cannot view couriers for that society.");
                return false;
            }

            societyId = requestedSocietyId is Guid value && value != Guid.Empty
                ? value
                : session.SocietyIds[0];
            return true;
        }

        private static bool TryResolveSocietyWrite(
            PlatformAdminSession session,
            Guid? requestedSocietyId,
            out Guid societyId,
            out ActionResult? error)
        {
            societyId = Guid.Empty;
            error = null;
            var isPlatform = string.Equals(session.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            if (isPlatform)
            {
                if (requestedSocietyId is null || requestedSocietyId == Guid.Empty)
                {
                    error = BadRequestResult("Select a society.");
                    return false;
                }

                societyId = requestedSocietyId.Value;
                return true;
            }

            if (session.SocietyIds.Count == 0)
            {
                error = ForbiddenResult("No society is linked to this admin.");
                return false;
            }

            if (requestedSocietyId is Guid asked && asked != Guid.Empty && !session.SocietyIds.Contains(asked))
            {
                error = ForbiddenResult("You cannot manage couriers for that society.");
                return false;
            }

            societyId = requestedSocietyId is Guid value && value != Guid.Empty
                ? value
                : session.SocietyIds[0];
            return true;
        }

        private async Task<ActionResult?> ValidateCreateAsync(
            CreateCourierAgentRequest request,
            Guid societyId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.CarrierName))
                return BadRequest(new { Message = "Courier name is required." });

            if (!await _db.Societies.AnyAsync(society => society.Id == societyId, cancellationToken).ConfigureAwait(false))
                return BadRequest(new { Message = "Selected society was not found." });

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.Phone, out var phone))
                    return BadRequest(new { Message = "Enter a valid 10-digit Indian mobile number." });

                var duplicate = await _db.CourierAgents.AsNoTracking()
                    .AnyAsync(agent => agent.SocietyId == societyId && agent.Phone == phone, cancellationToken)
                    .ConfigureAwait(false);
                if (duplicate)
                    return Conflict(new { Message = "A courier with this mobile number already exists in the society." });

                var blacklisted = await _db.CourierBlacklists.AsNoTracking()
                    .AnyAsync(
                        item => item.IsActive && item.SocietyId == societyId && item.Phone == phone,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (blacklisted)
                    return Conflict(new { Message = "This mobile number is blacklisted for the society." });

                return null;
            }

            var carrierName = request.CarrierName.Trim();
            var existingNames = await _db.CourierAgents.AsNoTracking()
                .Where(agent => agent.SocietyId == societyId)
                .Select(agent => agent.CarrierName)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (existingNames.Exists(name => string.Equals(name, carrierName, StringComparison.OrdinalIgnoreCase)))
                return Conflict(new { Message = "This courier name already exists in the society." });

            return null;
        }

        private static BadRequestObjectResult BadRequestResult(string message) =>
            new(new { Message = message });

        private static ObjectResult ForbiddenResult(string message) =>
            new(new { Message = message }) { StatusCode = StatusCodes.Status403Forbidden };

        private static bool PhonesMatch(string? left, string? right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            if (PhoneNumberGuard.TryNormalizeIndiaMobile(left, out var a) &&
                PhoneNumberGuard.TryNormalizeIndiaMobile(right, out var b))
            {
                return string.Equals(a, b, StringComparison.Ordinal);
            }

            return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static CourierAgentDto ToDto(CourierAgent agent, string societyName, CourierBlacklist? blacklist) =>
            new()
            {
                Id = agent.Id,
                SocietyId = agent.SocietyId,
                SocietyName = societyName,
                CarrierName = agent.CarrierName,
                AgentName = agent.AgentName,
                Phone = agent.Phone,
                VehicleNumber = agent.VehicleNumber,
                EmployeeOrBadgeNumber = agent.EmployeeOrBadgeNumber,
                IsActive = agent.IsActive,
                IsBlacklisted = blacklist is not null,
                BlacklistReason = blacklist?.Reason,
                CreatedAt = agent.CreatedAt,
            };
    }
}
