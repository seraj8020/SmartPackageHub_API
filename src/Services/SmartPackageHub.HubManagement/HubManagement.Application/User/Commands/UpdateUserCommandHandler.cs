using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

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
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(IRepository<User> users, ILogger<UpdateUserCommandHandler> logger, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _logger = logger;
        }

        public override async Task<UserCommandResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var user = await _users.GetByIdAsync(request.UserId).ConfigureAwait(false);
            if (user == null)
                return new UserCommandResult(false, false, false, "User was not found.", null);

            var updateResult = ApplyRequestedChanges(request, user);
            if (updateResult is not null)
                return updateResult;

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogUserUpdated(_logger, user.Id, null);
            return new UserCommandResult(true, false, true, "User updated.", UserDetailsDto.From(user));
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
            if (taken) return new UserCommandResult(true, true, false, "Username already exists.", null);
            user.ChangeUsername(username);
            return null;
        }

        private UserCommandResult? UpdateEmail(string? requestedEmail, User user)
        {
            if (string.IsNullOrWhiteSpace(requestedEmail)) return null;
            var email = requestedEmail.Trim();
            var taken = _users.GetAll().AsEnumerable().Any(existing => existing.Id != user.Id &&
                string.Equals(existing.ContactInfo?.Email, email, StringComparison.OrdinalIgnoreCase));
            if (taken) return new UserCommandResult(true, true, false, "Email already exists.", null);
            user.ChangeContact(ContactInfo.Create(email));
            return null;
        }

        private static UserCommandResult? UpdateRole(string? requestedRole, bool? isAdmin, User user)
        {
            if (string.IsNullOrWhiteSpace(requestedRole) && !isAdmin.HasValue) return null;
            var role = ResolveUpdatedRole(requestedRole, isAdmin, user.Role.Type);
            if (role is null) return new UserCommandResult(true, false, false, "Role must be Admin, Courier, Resident, or Staff.", null);
            user.ChangeRole(role);
            return null;
        }

        private UserCommandResult? UpdatePhone(string? requestedPhone, User user)
        {
            if (requestedPhone is null) return null;
            var phone = requestedPhone.Trim();
            var taken = _users.GetAll().AsEnumerable().Any(existing => existing.Id != user.Id &&
                string.Equals(existing.PhoneNumber, phone, StringComparison.OrdinalIgnoreCase));
            if (taken) return new UserCommandResult(true, true, false, "Phone already exists.", null);
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
