using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;


namespace HubManagement.Application.HubUsers.Commands
{
    public sealed record CreateUserCommand(string Username, string Email, string? Role, bool IsAdmin, string? Status) : ICreateCommand<UserCommandResult>;

    public sealed class CreateUserCommandHandler : CreateCommandHandler<CreateUserCommand, UserCommandResult>
    {
        private readonly IRepository<User> _users;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(IRepository<User> users, ILogger<CreateUserCommandHandler> logger, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _logger = logger;
        }

        public override async Task<UserCommandResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return new UserCommandResult(true, false, false, "Username is required", null);
            if (string.IsNullOrWhiteSpace(request.Email))
                return new UserCommandResult(true, false, false, "Email is required", null);

            if (UserExists(request.Username, request.Email, null))
                return new UserCommandResult(true, true, false, "A user with the same username or email already exists", null);

            var role = ResolveRole(request.Role, request.IsAdmin, RoleType.Staff);
            if (role == null)
                return new UserCommandResult(true, false, false, "Role must be Admin, Courier, Resident, or Staff.", null);

            var user = User.Create(request.Username.Trim(), ContactInfo.Create(request.Email.Trim()), role, string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim());
            _users.Insert(user);
            await UnitOfWork!.CommitAsync(cancellationToken);
            _logger.LogInformation("Created user {UserId}", user.Id);
            return new UserCommandResult(true, false, true, "User created.", UserDetailsDto.From(user));
        }

        private bool UserExists(string username, string email, Guid? exceptUserId)
        {
            return _users.GetAll().ToList().Any(user =>
                user.Id != exceptUserId &&
                (string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(user.ContactInfo?.Email, email, StringComparison.OrdinalIgnoreCase)));
        }

        internal static Role? ResolveRole(string? role, bool? isAdmin, RoleType fallback)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                if (!Enum.TryParse<RoleType>(role, ignoreCase: true, out var parsed))
                    return null;

                return Role.Create(parsed);
            }

            if (isAdmin == true)
                return Role.Create(RoleType.Admin);

            return Role.Create(fallback);
        }
    }
}
