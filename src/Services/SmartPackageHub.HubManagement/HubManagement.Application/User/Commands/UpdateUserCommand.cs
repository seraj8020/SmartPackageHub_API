using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubUsers.Commands
{
    public sealed record UpdateUserCommand(Guid UserId, string? Email, string? Role, bool? IsAdmin, string? Status) : IUpdateCommand<UserCommandResult>;

    public sealed class UpdateUserCommandHandler : UpdateCommandHandler<UpdateUserCommand, UserCommandResult>
    {
        private readonly IRepository<User> _users;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(IRepository<User> users, ILogger<UpdateUserCommandHandler> logger, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _logger = logger;
        }

        public override async Task<UserCommandResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _users.GetByIdAsync(request.UserId);
            if (user == null)
                return new UserCommandResult(false, false, false, "User was not found.", null);

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var emailTaken = _users.GetAll().ToList().Any(existing =>
                    existing.Id != user.Id &&
                    string.Equals(existing.ContactInfo?.Email, request.Email, StringComparison.OrdinalIgnoreCase));
                if (emailTaken)
                    return new UserCommandResult(true, true, false, "A user with the same username or email already exists", null);

                user.ChangeContact(ContactInfo.Create(request.Email.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(request.Role) || request.IsAdmin.HasValue)
            {
                var role = ResolveUpdatedRole(request.Role, request.IsAdmin, user.Role.Type);
                if (role == null)
                    return new UserCommandResult(true, false, false, "Role must be Admin, Courier, Resident, or Staff.", null);

                user.ChangeRole(role);
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
                user.ChangeStatus(request.Status);

            static Role? ResolveUpdatedRole(string? role, bool? isAdmin, RoleType current)
            {
                if (!string.IsNullOrWhiteSpace(role))
                    return CreateUserCommandHandler.ResolveRole(role, null, current);

                if (isAdmin == true)
                    return Role.Create(RoleType.Admin);

                if (isAdmin == false && current == RoleType.Admin)
                    return Role.Create(RoleType.Staff);

                return Role.Create(current);
            }

            await UnitOfWork!.CommitAsync(cancellationToken);
            _logger.LogInformation("Updated user {UserId}", user.Id);
            return new UserCommandResult(true, false, true, "User updated.", UserDetailsDto.From(user));
        }
    }
}
