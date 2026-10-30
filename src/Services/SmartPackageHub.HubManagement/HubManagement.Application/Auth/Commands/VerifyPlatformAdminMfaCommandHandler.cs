using System.Security.Cryptography;
using HubManagement.Application.Auth.DTOs;
using HubManagement.Application.Auth.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Auth.Commands
{
    public sealed class VerifyPlatformAdminMfaCommandHandler
        : CreateCommandHandler<VerifyPlatformAdminMfaCommand, PlatformAdminLoginResult>
    {
        private readonly IRepository<User> _users;
        private readonly IRepository<UserSociety> _userSocieties;
        private readonly PlatformAdminSessionStore _sessions;

        public VerifyPlatformAdminMfaCommandHandler(
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
            VerifyPlatformAdminMfaCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.MfaToken) || string.IsNullOrWhiteSpace(request.Code))
            {
                return Task.FromResult(new PlatformAdminLoginResult(
                    false, false, null, null, null, null, "MFA token and code are required."));
            }

            if (!_sessions.TryGetMfaChallenge(request.MfaToken.Trim(), out var challenge))
            {
                return Task.FromResult(new PlatformAdminLoginResult(
                    false, false, null, null, null, null, "Verification expired. Sign in again."));
            }

            if (challenge.Attempts >= 5)
            {
                _sessions.RemoveMfaChallenge(request.MfaToken.Trim());
                return Task.FromResult(new PlatformAdminLoginResult(
                    false, false, null, null, null, null, "Too many attempts. Sign in again."));
            }

            var expected = PlatformAdminLoginCommandHandler.HashCode(request.Code.Trim());
            var ok = CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expected),
                Convert.FromHexString(challenge.CodeHash));

            if (!ok)
            {
                _sessions.SetMfaChallenge(
                    request.MfaToken.Trim(),
                    challenge with { Attempts = challenge.Attempts + 1 });
                return Task.FromResult(new PlatformAdminLoginResult(
                    false, true, request.MfaToken.Trim(), null, null, null, "Invalid verification code."));
            }

            _sessions.RemoveMfaChallenge(request.MfaToken.Trim());

            var user = _users.GetAll().AsEnumerable()
                .FirstOrDefault(item => item.Id == challenge.UserId);
            if (user is null || user.Role.Type is not (RoleType.Admin or RoleType.PlatformAdmin))
            {
                return Task.FromResult(new PlatformAdminLoginResult(
                    false, false, null, null, null, null, "Admin account not found."));
            }

            var societyIds = _userSocieties.GetAll().AsEnumerable()
                .Where(link => link.UserId == user.Id)
                .Select(link => link.SocietyId)
                .Distinct()
                .ToList();

            var roleName = user.Role.Type.ToString();
            var sessionToken = AuthTokenFactory.CreateOpaqueToken();
            _sessions.SetSession(
                sessionToken,
                new PlatformAdminSession(
                    user.Id,
                    user.Username,
                    challenge.Email,
                    roleName,
                    societyIds));

            return Task.FromResult(new PlatformAdminLoginResult(
                true,
                false,
                null,
                challenge.Email,
                null,
                sessionToken,
                "Signed in.",
                roleName,
                user.Username,
                societyIds));
        }
    }
}
