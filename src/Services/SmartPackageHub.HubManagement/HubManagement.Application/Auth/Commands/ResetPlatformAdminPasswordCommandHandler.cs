using System.Security.Cryptography;
using HubManagement.Application.Auth.DTOs;
using HubManagement.Application.Auth.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Auth.Commands
{
    public sealed class ResetPlatformAdminPasswordCommandHandler
        : UpdateCommandHandler<ResetPlatformAdminPasswordCommand, PlatformAdminResetPasswordResult>
    {
        private readonly IRepository<User> _users;
        private readonly PlatformAdminSessionStore _sessions;

        public ResetPlatformAdminPasswordCommandHandler(
            IRepository<User> users,
            PlatformAdminSessionStore sessions,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _sessions = sessions;
        }

        public override async Task<PlatformAdminResetPasswordResult> Handle(
            ResetPlatformAdminPasswordCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.ResetToken) ||
                string.IsNullOrWhiteSpace(request.Code) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new PlatformAdminResetPasswordResult(
                    false, "Reset token, email code, and new password are required.");
            }

            var newPassword = request.NewPassword.Trim();
            if (newPassword.Length < 8)
                return new PlatformAdminResetPasswordResult(false, "New password must be at least 8 characters.");

            var resetToken = request.ResetToken.Trim();
            if (!_sessions.TryGetPasswordReset(resetToken, out var challenge))
                return new PlatformAdminResetPasswordResult(false, "Reset expired. Request a new code.");

            if (challenge.Attempts >= 5)
            {
                _sessions.RemovePasswordReset(resetToken);
                return new PlatformAdminResetPasswordResult(false, "Too many attempts. Request a new code.");
            }

            var code = request.Code.Trim();
            var expected = PlatformAdminLoginCommandHandler.HashCode(code);
            var ok = CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expected),
                Convert.FromHexString(challenge.CodeHash));

            if (!ok)
            {
                _sessions.SetPasswordReset(
                    resetToken,
                    challenge with { Attempts = challenge.Attempts + 1 });
                return new PlatformAdminResetPasswordResult(false, "Invalid verification code.");
            }

            var user = await _users.GetByIdAsync(challenge.UserId).ConfigureAwait(false);
            if (user is null || user.Role.Type is not (RoleType.Admin or RoleType.PlatformAdmin))
            {
                _sessions.RemovePasswordReset(resetToken);
                return new PlatformAdminResetPasswordResult(false, "Admin account not found.");
            }

            var hash = PasswordHasher.Hash(newPassword);
            var rows = await _users.ExecuteUpdatePropertyAsync(
                    u => u.Id == challenge.UserId,
                    u => u.PasswordHash,
                    hash,
                    cancellationToken)
                .ConfigureAwait(false);

            if (rows < 1)
                return new PlatformAdminResetPasswordResult(false, "Could not update password. Try again.");

            _sessions.RemovePasswordReset(resetToken);

            return new PlatformAdminResetPasswordResult(true, "Password reset. You can sign in with the new password.");
        }
    }
}
