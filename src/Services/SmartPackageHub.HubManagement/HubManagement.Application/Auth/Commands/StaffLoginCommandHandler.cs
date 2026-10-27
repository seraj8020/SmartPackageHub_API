using HubManagement.Application.Auth.DTOs;
using HubManagement.Application.Auth.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Auth.Commands
{
    public sealed class StaffLoginCommandHandler
        : CreateCommandHandler<StaffLoginCommand, StaffLoginResult>
    {
        private readonly IRepository<User> _users;
        private readonly IRepository<UserSociety> _userSocieties;

        public StaffLoginCommandHandler(
            IRepository<User> users,
            IRepository<UserSociety> userSocieties,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _userSocieties = userSocieties;
        }

        public override Task<StaffLoginResult> Handle(
            StaffLoginCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Task.FromResult(new StaffLoginResult(
                    false, null, null, null, null, Array.Empty<Guid>(), null,
                    "Username and password are required."));
            }

            var username = request.Username.Trim();
            var user = _users.GetAll().AsEnumerable()
                .FirstOrDefault(item =>
                    string.Equals(item.Username, username, StringComparison.OrdinalIgnoreCase));

            if (user is null ||
                !string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new StaffLoginResult(
                    false, null, null, null, null, Array.Empty<Guid>(), null,
                    "Invalid username or password."));
            }

            var role = user.Role.Type;
            if (role is not (RoleType.Staff or RoleType.Admin or RoleType.Courier))
            {
                return Task.FromResult(new StaffLoginResult(
                    false, null, null, null, null, Array.Empty<Guid>(), null,
                    "This account cannot sign in on the staff app. Use resident mobile OTP login."));
            }

            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Task.FromResult(new StaffLoginResult(
                    false, null, null, null, null, Array.Empty<Guid>(), null,
                    "Invalid username or password."));
            }

            var societyIds = _userSocieties.GetAll().AsEnumerable()
                .Where(link => link.UserId == user.Id)
                .Select(link => link.SocietyId)
                .Distinct()
                .ToList();

            return Task.FromResult(new StaffLoginResult(
                true,
                AuthTokenFactory.CreateOpaqueToken(),
                user.Id,
                user.Username,
                role.ToString(),
                societyIds,
                societyIds.FirstOrDefault(),
                "Signed in."));
        }
    }
}
