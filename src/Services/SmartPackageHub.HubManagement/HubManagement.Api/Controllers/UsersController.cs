using HubManagement.Application.HubUsers.Commands;
using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Application.HubUsers.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.ConfigOptions;
using HubManagement.Application.Auth.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.HubManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SmartPackageHub.Common.Services.Messaging.Guards;
using System.Net.Mail;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/users")]
    public class UsersController : ControllerBase
    {
        readonly IMediator _mediator;
        readonly PlatformAdminSessionStore _sessions;
        readonly HubDbContext _db;
        public UsersController(IMediator mediator, PlatformAdminSessionStore sessions, HubDbContext db)
        {
            _mediator = mediator;
            _sessions = sessions;
            _db = db;
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
            var isPlatform = string.Equals(session!.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            var roleError = ValidateInviteRole(request, session, isPlatform);
            if (roleError is not null) return roleError;

            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length < 2 || string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Password) ||
                request.Password.Trim().Length < 6)
                return BadRequest(new { Message = "Username, email, phone and a password of at least 6 characters are required." });
            if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.PhoneNumber, out var phoneNumber))
                return BadRequest(new { Message = "Enter a valid 10-digit Indian mobile number." });

            var societyId = isPlatform ? request.SocietyId!.Value : session!.SocietyIds[0];
            if (!await _db.Societies.AnyAsync(s => s.Id == societyId, cancellationToken).ConfigureAwait(false))
                return BadRequest(new { Message = "Selected society was not found." });

            var username = request.Username.Trim();
            var email = request.Email.Trim();
            var duplicateField = await FindDuplicateField(username, email, phoneNumber, cancellationToken).ConfigureAwait(false);
            if (duplicateField is not null)
                return Conflict(new { Field = duplicateField, Message = $"{duplicateField} already exists." });

            var transaction = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var user = global::HubManagement.Core.Hubs.Entities.User.Create(username, ContactInfo.Create(email),
                    Role.Create(isPlatform && string.Equals(request.Role?.Trim(), "Admin", StringComparison.OrdinalIgnoreCase) ? RoleType.Admin : RoleType.Staff),
                    "Active", PasswordHasher.Hash(request.Password.Trim()), phoneNumber);
                _db.Users.Add(user);
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _db.UsersSocieties.Add(UserSociety.Create(user.Id, societyId));
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                var societyName = await _db.Societies.AsNoTracking().Where(s => s.Id == societyId).Select(s => s.Name).SingleAsync(cancellationToken).ConfigureAwait(false);
                return StatusCode(StatusCodes.Status201Created, new UserDetailsDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.ContactInfo?.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role.Type.ToString(),
                    IsAdmin = user.Role.Type == RoleType.Admin,
                    Status = user.Status,
                    SocietyIds = new[] { societyId },
                    SocietyNames = new[] { societyName }
                });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return Conflict(new { Message = "Username, email or phone already exists." });
            }
            finally
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }

        private ActionResult? ValidateInviteRole(InviteUserRequest request, PlatformAdminSession session, bool isPlatform)
        {
            var requestedRole = request.Role?.Trim();
            if (isPlatform)
            {
                if (!string.Equals(requestedRole, "Admin", StringComparison.OrdinalIgnoreCase) && !string.Equals(requestedRole, "Staff", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { Message = "Platform admins can invite Society Admin or Staff only." });
                return request.SocietyId is null || request.SocietyId == Guid.Empty ? BadRequest(new { Message = "Select a society." }) : null;
            }
            if (!string.Equals(session.Role, "Admin", StringComparison.OrdinalIgnoreCase)) return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Admin access required." });
            if (!string.Equals(requestedRole, "Staff", StringComparison.OrdinalIgnoreCase)) return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Society admins can invite Staff only." });
            if (session.SocietyIds.Count == 0) return StatusCode(StatusCodes.Status403Forbidden, new { Message = "No society is linked to this admin." });
            return request.SocietyId.HasValue && !session.SocietyIds.Contains(request.SocietyId.Value) ? StatusCode(StatusCodes.Status403Forbidden, new { Message = "You cannot invite users to that society." }) : null;
        }

        [HttpGet("availability")]
        [MapToApiVersion(ApiVersions.V2)]
        public async Task<ActionResult> CheckAvailability([FromQuery] string field, [FromQuery] string value, [FromQuery] Guid? excludeUserId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(field);
            ArgumentNullException.ThrowIfNull(value);
            if (!TryGetSession(out var session, out var authError)) return authError!;
            if (!string.Equals(session!.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase) && !string.Equals(session.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Admin access required." });
            if (string.IsNullOrWhiteSpace(value)) return Ok(new { Available = true });
            if (!string.Equals(field, "username", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(field, "email", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(field, "phone", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Message = "Field must be username, email, or phone." });

            var normalized = value.Trim();
            var users = await _db.Users.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
            if (excludeUserId.HasValue) users = users.Where(user => user.Id != excludeUserId.Value).ToList();
            var fieldName = field.ToUpperInvariant();
            var exists = fieldName switch
            {
                "USERNAME" => users.Any(user => user.Role.Type != RoleType.Resident &&
                    string.Equals(user.Username, normalized, StringComparison.OrdinalIgnoreCase)),
                "EMAIL" => users.Any(user => string.Equals(user.ContactInfo?.Email, normalized, StringComparison.OrdinalIgnoreCase)),
                _ => users.Any(user => PhoneNumbersMatch(user.PhoneNumber, normalized))
            };
            if (string.Equals(fieldName, "PHONE", StringComparison.Ordinal))
            {
                var residentPhones = await _db.Residents.AsNoTracking()
                    .Where(resident => resident.PhoneNumber != null && (!excludeUserId.HasValue || resident.UserId != excludeUserId.Value))
                    .Select(resident => resident.PhoneNumber!)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                exists |= residentPhones.Any(phone => PhoneNumbersMatch(phone, normalized));
            }
            return Ok(new { Available = !exists });
        }

        private async Task<string?> FindDuplicateField(string username, string email, string phone, CancellationToken cancellationToken)
        {
            var users = await _db.Users.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
            if (users.Any(user => user.Role.Type != RoleType.Resident &&
                string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase))) return "Username";
            if (users.Any(user => string.Equals(user.ContactInfo?.Email, email, StringComparison.OrdinalIgnoreCase))) return "Email";
            if (users.Any(user => PhoneNumbersMatch(user.PhoneNumber, phone))) return "Phone";
            var residentPhones = await _db.Residents.AsNoTracking()
                .Where(resident => resident.PhoneNumber != null)
                .Select(resident => resident.PhoneNumber!)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            if (residentPhones.Any(existing => PhoneNumbersMatch(existing, phone))) return "Phone";
            return null;
        }

        private static bool PhoneNumbersMatch(string? left, string? right)
        {
            if (PhoneNumberGuard.TryNormalizeIndiaMobile(left, out var normalizedLeft) &&
                PhoneNumberGuard.TryNormalizeIndiaMobile(right, out var normalizedRight))
                return string.Equals(normalizedLeft, normalizedRight, StringComparison.Ordinal);

            return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
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

        [HttpGet]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<UserDetailsDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAll([FromQuery] bool residentsOnly = false)
        {
            if (!TryGetSession(out var session, out var authError)) return authError!;
            if (!string.Equals(session!.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(session.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Admin access required." });
            var users = await _mediator.Send(new GetUsersQuery()).ConfigureAwait(false);
            var isPlatformAdmin = string.Equals(session.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            Guid? societyId = null;
            if (!isPlatformAdmin)
            {
                societyId = session.SocietyIds.Count > 0 ? session.SocietyIds[0] : Guid.Empty;
            }
            if (societyId == Guid.Empty) return StatusCode(StatusCodes.Status403Forbidden, new { Message = "No society is linked to this admin." });

            var residentQuery = _db.Residents.AsNoTracking();
            if (!isPlatformAdmin) residentQuery = residentQuery.Where(resident => resident.SocietyId == societyId);
            var residentRows = await residentQuery.Join(
                _db.Users.AsNoTracking(), resident => resident.UserId, user => user.Id,
                (resident, user) => new { resident, user })
                .Join(_db.Societies.AsNoTracking(), item => item.resident.SocietyId, society => society.Id,
                (item, society) => new UserDetailsDto
                {
                    Id = item.resident.UserId,
                    Username = item.resident.FullName ?? item.user.Username,
                    Email = item.user.ContactInfo != null ? item.user.ContactInfo.Email : string.Empty,
                    PhoneNumber = item.resident.PhoneNumber,
                    Role = "Resident",
                    Status = item.resident.IsActive ? "Active" : "Not Active",
                    SocietyIds = new[] { item.resident.SocietyId },
                    SocietyNames = new[] { society.Name },
                    TowerWing = item.resident.TowerWing,
                    UnitNumber = item.resident.Unit != null ? item.resident.Unit.Value : string.Empty,
                    IsMember = item.resident.IsMember
                })
                .ToListAsync(HttpContext.RequestAborted).ConfigureAwait(false);

            if (residentsOnly) return Ok(residentRows);

            if (!isPlatformAdmin) users = users.Where(user => user.SocietyIds.Contains(societyId!.Value)).ToList();
            var residentUserIds = await _db.Residents.AsNoTracking().Select(resident => resident.UserId).ToListAsync(HttpContext.RequestAborted).ConfigureAwait(false);
            return Ok(users.Where(user => !residentUserIds.Contains(user.Id)).ToList());
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
            if (!string.Equals(session!.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(session.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Admin access required." });
            var validationError = ValidateResidentEdit(request, out var phone);
            if (validationError is not null) return validationError;

            var resident = await _db.Residents.FirstOrDefaultAsync(item => item.UserId == id, cancellationToken).ConfigureAwait(false);
            if (resident is null) return NotFound();
            var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken).ConfigureAwait(false);
            if (user is null) return NotFound();
            var platformAdmin = string.Equals(session.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            var targetSocietyId = platformAdmin ? request.SocietyId : resident.SocietyId;
            if (!platformAdmin && !session.SocietyIds.Contains(resident.SocietyId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Society admins can edit residents in their society only." });
            if (targetSocietyId is null || targetSocietyId == Guid.Empty ||
                !await _db.Societies.AnyAsync(item => item.Id == targetSocietyId, cancellationToken).ConfigureAwait(false))
                return BadRequest(new { Message = "Select a valid society." });

            var username = request.Username.Trim();
            var existingUsers = await _db.Users.AsNoTracking().Where(item => item.Id != id).ToListAsync(cancellationToken).ConfigureAwait(false);
            var duplicateUserPhone = existingUsers.Any(item => PhoneNumbersMatch(item.PhoneNumber, phone));
            var existingResidentPhones = await _db.Residents.AsNoTracking()
                .Where(item => item.UserId != id && item.PhoneNumber != null)
                .Select(item => item.PhoneNumber!)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var duplicateResidentPhone = existingResidentPhones.Any(existing => PhoneNumbersMatch(existing, phone));
            if (duplicateUserPhone || duplicateResidentPhone)
                return Conflict(new { Field = "Phone", Message = "Phone already exists." });

            var isActive = string.Equals(request.Status, "Active", StringComparison.Ordinal);
            resident.UpdateAdminManagedDetails(username, phone, request.TowerWing, request.UnitNumber, isActive, targetSocietyId.Value);
            user.ChangeUsername(username);
            user.SetPhoneNumber(phone);
            user.ChangeStatus(request.Status);
            if (platformAdmin)
                await SyncResidentSocietyLink(id, targetSocietyId.Value, cancellationToken).ConfigureAwait(false);
            var society = await _db.Societies.AsNoTracking().FirstAsync(item => item.Id == targetSocietyId.Value, cancellationToken).ConfigureAwait(false);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Ok(new UserDetailsDto
            {
                Id = resident.UserId,
                Username = user.Username,
                Email = user.ContactInfo?.Email ?? string.Empty,
                PhoneNumber = resident.PhoneNumber,
                Role = "Resident",
                Status = resident.IsActive ? "Active" : "Not Active",
                SocietyIds = new[] { resident.SocietyId },
                SocietyNames = new[] { society.Name },
                TowerWing = resident.TowerWing,
                UnitNumber = resident.Unit?.Value ?? string.Empty,
                IsMember = resident.IsMember
            });
        }

        private BadRequestObjectResult? ValidateResidentEdit(UpdateResidentRequest request, out string phone)
        {
            phone = string.Empty;
            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length > 100 ||
                string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.TowerWing) || request.TowerWing.Trim().Length > 50 || string.IsNullOrWhiteSpace(request.UnitNumber) ||
                (!string.Equals(request.Status, "Active", StringComparison.Ordinal) && !string.Equals(request.Status, "Not Active", StringComparison.Ordinal)))
                return BadRequest(new { Message = "Username, phone number, tower or wing, flat, and a valid status are required." });
            if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.PhoneNumber, out phone))
                return BadRequest(new { Message = "Enter a valid 10-digit Indian mobile number." });
            try
            {
                _ = UnitNumber.Create(request.UnitNumber);
                return null;
            }
            catch (ArgumentException)
            {
                return BadRequest(new { Message = "Enter a valid flat number." });
            }
        }

        private async Task SyncResidentSocietyLink(Guid userId, Guid societyId, CancellationToken cancellationToken)
        {
            var currentLinks = await _db.UsersSocieties.Where(link => link.UserId == userId).ToListAsync(cancellationToken).ConfigureAwait(false);
            _db.UsersSocieties.RemoveRange(currentLinks);
            _db.UsersSocieties.Add(UserSociety.Create(userId, societyId));
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
        public async Task<ActionResult> Create([FromBody] CreateUserRequest request)
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
                SocietyId: request.SocietyId)).ConfigureAwait(false);
            if (result.Conflict)
                return Conflict(new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            if (result.User is not null)
            {
                result.User.SocietyIds = new[] { request.SocietyId.Value };
                result.User.SocietyNames = await _db.Societies.AsNoTracking()
                    .Where(society => society.Id == request.SocietyId.Value)
                    .Select(society => society.Name)
                    .ToListAsync(HttpContext.RequestAborted).ConfigureAwait(false);
            }
            return Ok(result.User);
        }

        [HttpPut("{id:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            if (!TryGetSession(out var session, out var authError)) return authError!;
            ArgumentNullException.ThrowIfNull(request);
            var fieldValidationError = ValidateAndNormalizeUserUpdateFields(request);
            if (fieldValidationError is not null) return fieldValidationError;
            if (request.PhoneNumber is not null)
            {
                if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.PhoneNumber, out var normalizedPhone))
                    return BadRequest(new { Message = "Enter a valid 10-digit Indian mobile number." });
                request.PhoneNumber = normalizedPhone;
            }
            var target = await _db.Users.Include(user => user.Societies).FirstOrDefaultAsync(user => user.Id == id, HttpContext.RequestAborted).ConfigureAwait(false);
            if (target is null) return NotFound();
            var platformAdmin = string.Equals(session!.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            var societyAdmin = string.Equals(session.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            var validationError = await ValidateUserUpdateRequest(request, target, session, platformAdmin, societyAdmin, HttpContext.RequestAborted).ConfigureAwait(false);
            if (validationError is not null) return validationError;
            var phoneConflict = await FindPhoneConflict(id, request.PhoneNumber, HttpContext.RequestAborted).ConfigureAwait(false);
            if (phoneConflict) return Conflict(new { Field = "Phone", Message = "Phone already exists." });
            if (platformAdmin)
            {
                _db.UsersSocieties.RemoveRange(target.Societies);
                _db.UsersSocieties.Add(UserSociety.Create(target.Id, request.SocietyId!.Value));
            }
            var result = await _mediator.Send(new UpdateUserCommand(
                id,
                request.Username,
                request.Email,
                request.Role,
                request.IsAdmin,
                request.Status,
                request.PhoneNumber,
                request.SocietyIds)).ConfigureAwait(false);
            if (!result.Found)
            {
                _db.ChangeTracker.Clear();
                return NotFound();
            }
            if (result.Conflict)
            {
                _db.ChangeTracker.Clear();
                return Conflict(new { Message = result.Message });
            }
            if (!result.Succeeded)
            {
                _db.ChangeTracker.Clear();
                return BadRequest(new { Message = result.Message });
            }

            if (platformAdmin && result.User is not null)
            {
                result.User.SocietyIds = new[] { request.SocietyId!.Value };
                result.User.SocietyNames = await _db.Societies.AsNoTracking()
                    .Where(society => society.Id == request.SocietyId.Value)
                    .Select(society => society.Name)
                    .ToListAsync(HttpContext.RequestAborted).ConfigureAwait(false);
            }
            return Ok(result.User);
        }

        private BadRequestObjectResult? ValidateAndNormalizeUserUpdateFields(UpdateUserRequest request)
        {
            if (request.Email is not null)
            {
                var email = request.Email.Trim();
                if (email.Length == 0 || !MailAddress.TryCreate(email, out var parsedEmail) ||
                    !string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { Field = "Email", Message = "Enter a valid email address." });
                request.Email = email;
            }

            if (request.Status is not null)
            {
                var status = request.Status.Trim();
                if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(status, "Not Active", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { Field = "Status", Message = "Status must be Active or Not Active." });
                request.Status = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) ? "Active" : "Not Active";
            }

            return null;
        }

        private async Task<ActionResult?> ValidateUserUpdateRequest(
            UpdateUserRequest request,
            global::HubManagement.Core.Hubs.Entities.User target,
            PlatformAdminSession session,
            bool platformAdmin,
            bool societyAdmin,
            CancellationToken cancellationToken)
        {
            if (!platformAdmin && !societyAdmin) return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Admin access required." });
            if (target.Role.Type is RoleType.PlatformAdmin or RoleType.Resident)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "This account cannot be edited here." });
            if (societyAdmin && (target.Role.Type != RoleType.Staff || !target.Societies.Any(link => session.SocietyIds.Contains(link.SocietyId))))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Society admins can edit Staff accounts in their society only." });
            if (societyAdmin && request.IsAdmin == true)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Society admins cannot promote users to admin." });
            if (request.Role is not null && !string.Equals(request.Role, "Staff", StringComparison.OrdinalIgnoreCase) &&
                !(platformAdmin && string.Equals(request.Role, "Admin", StringComparison.OrdinalIgnoreCase)))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "The requested role cannot be assigned by this admin." });
            if (platformAdmin && (!request.SocietyId.HasValue || request.SocietyId == Guid.Empty))
                return BadRequest(new { Message = "Select a society." });
            if (platformAdmin && !await _db.Societies.AnyAsync(society => society.Id == request.SocietyId, cancellationToken).ConfigureAwait(false))
                return BadRequest(new { Message = "Selected society was not found." });
            if (societyAdmin && request.SocietyId.HasValue && !session.SocietyIds.Contains(request.SocietyId.Value))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You cannot move this user to another society." });
            return null;
        }

        private async Task<bool> FindPhoneConflict(Guid excludedUserId, string? phone, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            var userPhones = await _db.Users.AsNoTracking().Where(user => user.Id != excludedUserId && user.PhoneNumber != null)
                .Select(user => user.PhoneNumber!).ToListAsync(cancellationToken).ConfigureAwait(false);
            if (userPhones.Any(existing => PhoneNumbersMatch(existing, phone))) return true;
            var residentPhones = await _db.Residents.AsNoTracking().Where(resident => resident.UserId != excludedUserId && resident.PhoneNumber != null)
                .Select(resident => resident.PhoneNumber!).ToListAsync(cancellationToken).ConfigureAwait(false);
            return residentPhones.Any(existing => PhoneNumbersMatch(existing, phone));
        }

    }

}
