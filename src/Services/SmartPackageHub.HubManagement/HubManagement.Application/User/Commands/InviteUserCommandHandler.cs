using HubManagement.Application.Auth.Services;
using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Application.HubUsers.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Guards;

namespace HubManagement.Application.HubUsers.Commands
{
    public sealed class InviteUserCommandHandler : CreateCommandHandler<InviteUserCommand, UserCommandResult>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogInvitedUser =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(InviteUserCommandHandler)),
                "Invited user {UserId}");

        private readonly IRepository<User> _users;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<UserSociety> _userSocieties;
        private readonly ILogger<InviteUserCommandHandler> _logger;

        public InviteUserCommandHandler(
            IRepository<User> users,
            IRepository<Resident> residents,
            IRepository<Society> societies,
            IRepository<UserSociety> userSocieties,
            ILogger<InviteUserCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _residents = residents;
            _societies = societies;
            _userSocieties = userSocieties;
            _logger = logger;
        }

        public override async Task<UserCommandResult> Handle(InviteUserCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var isPlatform = string.Equals(request.CallerRole, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            var roleError = ValidateInviteRole(request, isPlatform);
            if (roleError is not null)
                return roleError;

            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length < 2 ||
                string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.PhoneNumber) ||
                string.IsNullOrWhiteSpace(request.Password) || request.Password.Trim().Length < 6)
            {
                return new UserCommandResult(true, false, false, false,
                    "Username, email, phone and a password of at least 6 characters are required.", null);
            }

            if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.PhoneNumber, out var phoneNumber))
            {
                return new UserCommandResult(true, false, false, false,
                    "Enter a valid 10-digit Indian mobile number.", null);
            }

            var societyId = isPlatform ? request.SocietyId!.Value : request.CallerSocietyIds[0];
            if (await _societies.GetByIdAsync(societyId).ConfigureAwait(false) is null)
            {
                return new UserCommandResult(true, false, false, false, "Selected society was not found.", null);
            }

            var username = request.Username.Trim();
            var email = request.Email.Trim();
            var duplicateField = UserPhoneMatcher.FindDuplicateField(_users, _residents, username, email, phoneNumber);
            if (duplicateField is not null)
            {
                return new UserCommandResult(true, true, false, false, $"{duplicateField} already exists.", null, duplicateField);
            }

            var inviteRole = isPlatform && string.Equals(request.Role?.Trim(), "Admin", StringComparison.OrdinalIgnoreCase)
                ? RoleType.Admin
                : RoleType.Staff;
            var user = User.Create(
                username,
                ContactInfo.Create(email),
                Role.Create(inviteRole),
                "Active",
                PasswordHasher.Hash(request.Password.Trim()),
                phoneNumber);
            _users.Insert(user);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);

            _userSocieties.Insert(UserSociety.Create(user.Id, societyId));
            await UnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

            LogInvitedUser(_logger, user.Id, null);
            var dto = UserDetailsDtoMapper.FromUser(user, _userSocieties, _societies);
            return new UserCommandResult(true, false, true, false, "User invited.", dto);
        }

        private static UserCommandResult? ValidateInviteRole(InviteUserCommand request, bool isPlatform)
        {
            var requestedRole = request.Role?.Trim();
            if (isPlatform)
            {
                if (!string.Equals(requestedRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(requestedRole, "Staff", StringComparison.OrdinalIgnoreCase))
                {
                    return new UserCommandResult(true, false, false, false,
                        "Platform admins can invite Society Admin or Staff only.", null);
                }

                if (request.SocietyId is null || request.SocietyId == Guid.Empty)
                {
                    return new UserCommandResult(true, false, false, false, "Select a society.", null);
                }

                return null;
            }

            if (!string.Equals(request.CallerRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return new UserCommandResult(true, false, false, true, "Admin access required.", null);
            }

            if (!string.Equals(requestedRole, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                return new UserCommandResult(true, false, false, true, "Society admins can invite Staff only.", null);
            }

            if (request.CallerSocietyIds.Count == 0)
            {
                return new UserCommandResult(true, false, false, true, "No society is linked to this admin.", null);
            }

            if (request.SocietyId.HasValue && !request.CallerSocietyIds.Contains(request.SocietyId.Value))
            {
                return new UserCommandResult(true, false, false, true, "You cannot invite users to that society.", null);
            }

            return null;
        }
    }
}