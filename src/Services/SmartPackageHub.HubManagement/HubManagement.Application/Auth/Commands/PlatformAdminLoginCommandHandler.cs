using System.Security.Cryptography;
using System.Text;
using HubManagement.Application.Auth.DTOs;
using HubManagement.Application.Auth.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Auth.Commands
{
    public sealed class PlatformAdminLoginCommandHandler
        : CreateCommandHandler<PlatformAdminLoginCommand, PlatformAdminLoginResult>
    {
        private readonly IRepository<User> _users;
        private readonly IRepository<UserSociety> _userSocieties;
        private readonly PlatformAdminSessionStore _sessions;

        public PlatformAdminLoginCommandHandler(
            IRepository<User> users,
            IRepository<UserSociety> userSocieties,
            PlatformAdminSessionStore sessions,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _userSocieties = userSocieties;
            _sessions = sessions;
        }

        public override Task<PlatformAdminLoginResult> Handle(
            PlatformAdminLoginCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Task.FromResult(Fail("Username and password are required."));
            }

            var username = request.Username.Trim();
            var password = request.Password.Trim();
            var user = _users.GetAll().AsEnumerable()
                .FirstOrDefault(item =>
                    string.Equals(item.Username, username, StringComparison.OrdinalIgnoreCase));

            // PlatformAdmin or society Admin only. Staff/Resident/Courier rejected.
            if (user is null ||
                !string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase) ||
                user.Role.Type is not (RoleType.Admin or RoleType.PlatformAdmin) ||
                !PasswordHasher.Verify(password, user.PasswordHash))
            {
                return Task.FromResult(Fail("Invalid username or password."));
            }

            var societyIds = _userSocieties.GetAll().AsEnumerable()
                .Where(link => link.UserId == user.Id)
                .Select(link => link.SocietyId)
                .Distinct()
                .ToList();

            var isPlatformAdmin = user.Role.Type == RoleType.PlatformAdmin;
            if (!isPlatformAdmin && societyIds.Count == 0)
            {
                return Task.FromResult(Fail(
                    "This society admin is not linked to any society. Contact a platform admin."));
            }

            var email = user.ContactInfo?.Email ?? string.Empty;
            var roleName = user.Role.Type.ToString();
            var sessionToken = AuthTokenFactory.CreateOpaqueToken();
            _sessions.SetSession(
                sessionToken,
                new PlatformAdminSession(
                    user.Id,
                    user.Username,
                    email,
                    roleName,
                    societyIds));

            return Task.FromResult(new PlatformAdminLoginResult(
                true,
                false,
                null,
                string.IsNullOrWhiteSpace(email) ? null : MaskEmail(email),
                null,
                sessionToken,
                "Signed in.",
                roleName,
                user.Username,
                societyIds));
        }

        /// <summary>Shared by password-reset email code verification.</summary>
        internal static string HashCode(string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(bytes);
        }

        private static PlatformAdminLoginResult Fail(string message) =>
            new(false, false, null, null, null, null, message);

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1)
                return "***";
            return $"{email[0]}***{email[(at - 1)..]}";
        }
    }
}
