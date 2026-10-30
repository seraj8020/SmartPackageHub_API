using HubManagement.Application.Auth.DTOs;
using HubManagement.Application.Auth.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Auth.Commands
{
    public sealed class ChangePlatformAdminPasswordCommandHandler
        : UpdateCommandHandler<ChangePlatformAdminPasswordCommand, PlatformAdminPasswordChangeResult>
    {
        private readonly IRepository<User> _users;
        private readonly PlatformAdminSessionStore _sessions;

        public ChangePlatformAdminPasswordCommandHandler(
            IRepository<User> users,
            PlatformAdminSessionStore sessions,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _sessions = sessions;
        }

        public override async Task<PlatformAdminPasswordChangeResult> Handle(
            ChangePlatformAdminPasswordCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.AccessToken))
                return new PlatformAdminPasswordChangeResult(false, true, "Sign in required.");

            if (!_sessions.TryGetSession(request.AccessToken.Trim(), out var session))
                return new PlatformAdminPasswordChangeResult(false, true, "Session expired. Sign in again.");

            var currentPassword = request.CurrentPassword?.Trim() ?? string.Empty;
            var newPassword = request.NewPassword?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return new PlatformAdminPasswordChangeResult(false, false, "Current and new passwords are required.");

            if (newPassword.Length < 8)
                return new PlatformAdminPasswordChangeResult(false, false, "New password must be at least 8 characters.");

            if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
                return new PlatformAdminPasswordChangeResult(false, false, "New password must be different from the current password.");

            var user = await _users.GetByIdAsync(session.UserId).ConfigureAwait(false);
            if (user is null || user.Role.Type is not (RoleType.Admin or RoleType.PlatformAdmin))
                return new PlatformAdminPasswordChangeResult(false, true, "Admin account not found.");

            if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
                return new PlatformAdminPasswordChangeResult(false, false, "Current password is incorrect.");

            var hash = PasswordHasher.Hash(newPassword);
            var rows = await _users.ExecuteUpdatePropertyAsync(
                    u => u.Id == session.UserId,
                    u => u.PasswordHash,
                    hash,
                    cancellationToken)
                .ConfigureAwait(false);

            if (rows < 1)
                return new PlatformAdminPasswordChangeResult(false, false, "Could not update password. Try again.");

            // Force re-login with the new password.
            _sessions.RemoveSession(request.AccessToken.Trim());

            return new PlatformAdminPasswordChangeResult(
                true, false, "Password updated. Sign in again with your new password.");
        }
    }
}
