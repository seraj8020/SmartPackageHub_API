using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Application.HubUsers.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Guards;
using System.Net.Mail;

namespace HubManagement.Application.HubUsers.Commands
{
    public sealed class UpdateUserCommandHandler : UpdateCommandHandler<UpdateUserCommand, UserCommandResult>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogUserUpdated =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(UpdateUserCommandHandler)),
                "Updated user {UserId}");

        private readonly IRepository<User> _users;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<UserSociety> _userSocieties;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(
            IRepository<User> users,
            IRepository<Resident> residents,
            IRepository<Society> societies,
            IRepository<UserSociety> userSocieties,
            ILogger<UpdateUserCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _residents = residents;
            _societies = societies;
            _userSocieties = userSocieties;
            _logger = logger;
        }

        public override async Task<UserCommandResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var user = await _users.GetByIdAsync(request.UserId).ConfigureAwait(false);
            if (user == null)
                return new UserCommandResult(false, false, false, false, "User was not found.", null);

            var platformAdmin = string.Equals(request.CallerRole, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            var societyAdmin = string.Equals(request.CallerRole, "Admin", StringComparison.OrdinalIgnoreCase);

            var authorizationError = ValidateAuthorization(request, user, platformAdmin, societyAdmin);
            if (authorizationError is not null)
                return authorizationError;

            var fieldError = ValidateFields(request, out var normalizedEmail, out var normalizedStatus, out var normalizedPhone);
            if (fieldError is not null)
                return fieldError;

            request = request with
            {
                Email = normalizedEmail ?? request.Email,
                Status = normalizedStatus ?? request.Status,
                PhoneNumber = normalizedPhone ?? request.PhoneNumber
            };

            if (request.PhoneNumber is not null &&
                UserPhoneMatcher.PhoneExists(_users, _residents, request.UserId, request.PhoneNumber))
            {
                return new UserCommandResult(true, true, false, false, "Phone already exists.", null, "Phone");
            }

            if (platformAdmin && request.SocietyId.HasValue)
                ReplaceSocietyLink(request.UserId, request.SocietyId.Value);

            var updateResult = ApplyRequestedChanges(request, user);
            if (updateResult is not null)
                return updateResult;

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogUserUpdated(_logger, user.Id, null);

            var dto = UserDetailsDtoMapper.FromUser(user, _userSocieties, _societies);
            if (platformAdmin && request.SocietyId.HasValue)
            {
                dto.SocietyIds = new[] { request.SocietyId.Value };
                UserDetailsDtoMapper.EnrichSocietyNames(dto, _societies);
            }

            return new UserCommandResult(true, false, true, false, "User updated.", dto);
        }

        private UserCommandResult? ValidateAuthorization(
            UpdateUserCommand request,
            User target,
            bool platformAdmin,
            bool societyAdmin)
        {
            if (!platformAdmin && !societyAdmin)
                return new UserCommandResult(true, false, false, true, "Admin access required.", null);

            if (target.Role.Type is RoleType.PlatformAdmin or RoleType.Resident)
                return new UserCommandResult(true, false, false, true, "This account cannot be edited here.", null);

            var societyIds = request.CallerSocietyIds ?? Array.Empty<Guid>();
            var userSocietyIds = _userSocieties.GetAll().AsEnumerable()
                .Where(link => link.UserId == target.Id)
                .Select(link => link.SocietyId)
                .ToHashSet();

            if (societyAdmin && (target.Role.Type != RoleType.Staff ||
                !userSocietyIds.Any(societyIds.Contains)))
            {
                return new UserCommandResult(true, false, false, true,
                    "Society admins can edit Staff accounts in their society only.", null);
            }

            if (societyAdmin && request.IsAdmin == true)
            {
                return new UserCommandResult(true, false, false, true,
                    "Society admins cannot promote users to admin.", null);
            }

            if (request.Role is not null &&
                !string.Equals(request.Role, "Staff", StringComparison.OrdinalIgnoreCase) &&
                !(platformAdmin && string.Equals(request.Role, "Admin", StringComparison.OrdinalIgnoreCase)))
            {
                return new UserCommandResult(true, false, false, true,
                    "The requested role cannot be assigned by this admin.", null);
            }

            if (platformAdmin && (!request.SocietyId.HasValue || request.SocietyId == Guid.Empty))
                return new UserCommandResult(true, false, false, false, "Select a society.", null);

            if (platformAdmin && request.SocietyId.HasValue &&
                _societies.GetAll().AsEnumerable().All(society => society.Id != request.SocietyId.Value))
            {
                return new UserCommandResult(true, false, false, false, "Selected society was not found.", null);
            }

            if (societyAdmin && request.SocietyId.HasValue &&
                !(request.CallerSocietyIds ?? Array.Empty<Guid>()).Contains(request.SocietyId.Value))
            {
                return new UserCommandResult(true, false, false, true,
                    "You cannot move this user to another society.", null);
            }

            return null;
        }

        private static UserCommandResult? ValidateFields(
            UpdateUserCommand request,
            out string? normalizedEmail,
            out string? normalizedStatus,
            out string? normalizedPhone)
        {
            normalizedEmail = null;
            normalizedStatus = null;
            normalizedPhone = null;

            if (request.Email is not null)
            {
                var email = request.Email.Trim();
                if (email.Length == 0 || !MailAddress.TryCreate(email, out var parsedEmail) ||
                    !string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase))
                {
                    return new UserCommandResult(true, false, false, false, "Enter a valid email address.", null, "Email");
                }

                normalizedEmail = email;
            }

            if (request.Status is not null)
            {
                var status = request.Status.Trim();
                if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(status, "Not Active", StringComparison.OrdinalIgnoreCase))
                {
                    return new UserCommandResult(true, false, false, false, "Status must be Active or Not Active.", null, "Status");
                }

                normalizedStatus = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) ? "Active" : "Not Active";
            }

            if (request.PhoneNumber is not null)
            {
                if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.PhoneNumber, out var phone))
                {
                    return new UserCommandResult(true, false, false, false,
                        "Enter a valid 10-digit Indian mobile number.", null);
                }

                normalizedPhone = phone;
            }

            return null;
        }

        private void ReplaceSocietyLink(Guid userId, Guid societyId)
        {
            var currentLinks = _userSocieties.GetAll().AsEnumerable().Where(link => link.UserId == userId).ToList();
            if (currentLinks.Count > 0)
                _userSocieties.Remove(currentLinks);
            _userSocieties.Insert(UserSociety.Create(userId, societyId));
        }

        private UserCommandResult? ApplyRequestedChanges(UpdateUserCommand request, User user)
        {
            var usernameResult = UpdateUsername(request.Username, user);
            if (usernameResult is not null) return usernameResult;
            var emailResult = UpdateEmail(request.Email, user);
            if (emailResult is not null) return emailResult;
            var roleResult = UpdateRole(request.Role, request.IsAdmin, user);
            if (roleResult is not null) return roleResult;
            if (!string.IsNullOrWhiteSpace(request.Status)) user.ChangeStatus(request.Status);
            var phoneResult = UpdatePhone(request.PhoneNumber, user);
            if (phoneResult is not null) return phoneResult;
            LinkSocieties(request.SocietyIds, user);
            return null;
        }

        private UserCommandResult? UpdateUsername(string? requestedUsername, User user)
        {
            if (string.IsNullOrWhiteSpace(requestedUsername)) return null;
            var username = requestedUsername.Trim();
            var taken = user.Role.Type != RoleType.Resident && _users.GetAll().AsEnumerable().Any(existing =>
                existing.Id != user.Id && existing.Role.Type != RoleType.Resident &&
                string.Equals(existing.Username, username, StringComparison.OrdinalIgnoreCase));
            if (taken) return new UserCommandResult(true, true, false, false, "Username already exists.", null);
            user.ChangeUsername(username);
            return null;
        }

        private UserCommandResult? UpdateEmail(string? requestedEmail, User user)
        {
            if (string.IsNullOrWhiteSpace(requestedEmail)) return null;
            var email = requestedEmail.Trim();
            var taken = _users.GetAll().AsEnumerable().Any(existing => existing.Id != user.Id &&
                string.Equals(existing.ContactInfo?.Email, email, StringComparison.OrdinalIgnoreCase));
            if (taken) return new UserCommandResult(true, true, false, false, "Email already exists.", null);
            user.ChangeContact(ContactInfo.Create(email));
            return null;
        }

        private static UserCommandResult? UpdateRole(string? requestedRole, bool? isAdmin, User user)
        {
            if (string.IsNullOrWhiteSpace(requestedRole) && !isAdmin.HasValue) return null;
            var role = ResolveUpdatedRole(requestedRole, isAdmin, user.Role.Type);
            if (role is null) return new UserCommandResult(true, false, false, false, "Role must be Admin, Courier, Resident, or Staff.", null);
            user.ChangeRole(role);
            return null;
        }

        private static UserCommandResult? UpdatePhone(string? requestedPhone, User user)
        {
            if (requestedPhone is null) return null;
            user.SetPhoneNumber(requestedPhone);
            return null;
        }

        private static void LinkSocieties(IReadOnlyList<Guid>? societyIds, User user)
        {
            if (societyIds is not { Count: > 0 }) return;
            var linked = user.Societies.Select(society => society.SocietyId).ToHashSet();
            foreach (var societyId in societyIds.Distinct())
            {
                if (societyId == Guid.Empty || !linked.Add(societyId)) continue;
                user.LinkToSociety(societyId);
            }
        }

        private static Role? ResolveUpdatedRole(string? role, bool? isAdmin, RoleType current)
        {
            if (!string.IsNullOrWhiteSpace(role))
                return CreateUserCommandHandler.ResolveRole(role, null, current);
            if (isAdmin == true) return Role.Create(RoleType.Admin);
            if (isAdmin == false && current == RoleType.Admin) return Role.Create(RoleType.Staff);
            return Role.Create(current);
        }
    }
}