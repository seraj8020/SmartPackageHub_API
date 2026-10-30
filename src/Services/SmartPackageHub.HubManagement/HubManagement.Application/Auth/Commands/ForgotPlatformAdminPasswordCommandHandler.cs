using System.Globalization;
using System.Security.Cryptography;
using HubManagement.Application.Auth.DTOs;
using HubManagement.Application.Auth.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Models;

namespace HubManagement.Application.Auth.Commands
{
    public sealed class ForgotPlatformAdminPasswordCommandHandler
        : CreateCommandHandler<ForgotPlatformAdminPasswordCommand, PlatformAdminForgotPasswordResult>
    {
        private readonly IRepository<User> _users;
        private readonly IEmailSender _emailSender;
        private readonly PlatformAdminSessionStore _sessions;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<ForgotPlatformAdminPasswordCommandHandler> _logger;

        public ForgotPlatformAdminPasswordCommandHandler(
            IRepository<User> users,
            IEmailSender emailSender,
            PlatformAdminSessionStore sessions,
            IHostEnvironment environment,
            ILogger<ForgotPlatformAdminPasswordCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _emailSender = emailSender;
            _sessions = sessions;
            _environment = environment;
            _logger = logger;
        }

        public override async Task<PlatformAdminForgotPasswordResult> Handle(
            ForgotPlatformAdminPasswordCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            const string genericMessage =
                "If an admin account matches, a reset code was sent to the registered email.";

            if (string.IsNullOrWhiteSpace(request.UsernameOrEmail))
            {
                return new PlatformAdminForgotPasswordResult(
                    false, null, null, null, "Enter your username or email.");
            }

            var key = request.UsernameOrEmail.Trim();
            var user = _users.GetAll().AsEnumerable()
                .FirstOrDefault(item =>
                    item.Role.Type is (RoleType.Admin or RoleType.PlatformAdmin) &&
                    string.Equals(item.Status, "Active", StringComparison.OrdinalIgnoreCase) &&
                    (string.Equals(item.Username, key, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(item.ContactInfo?.Email, key, StringComparison.OrdinalIgnoreCase)));

            if (user is null || string.IsNullOrWhiteSpace(user.ContactInfo?.Email))
            {
                // Avoid account enumeration in non-dev environments.
                if (_environment.IsDevelopment())
                {
                    return new PlatformAdminForgotPasswordResult(
                        false, null, null, null, "No active admin matched that username/email.");
                }

                return new PlatformAdminForgotPasswordResult(true, null, null, null, genericMessage);
            }

            var email = user.ContactInfo.Email;
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000)
                .ToString("D6", CultureInfo.InvariantCulture);
            var resetToken = AuthTokenFactory.CreateOpaqueToken();
            _sessions.SetPasswordReset(
                resetToken,
                new PlatformAdminPasswordResetChallenge(
                    user.Id,
                    user.Username,
                    email,
                    PlatformAdminLoginCommandHandler.HashCode(code),
                    Attempts: 0));

            var send = await _emailSender.SendAsync(
                new EmailSendRequest(
                    email,
                    "Smart Package Hub — Platform Admin password reset",
                    $"Your password reset code is {code}. It expires in 10 minutes. If you did not request this, ignore this email.",
                    $"platform-reset:{user.Id:N}:{code}"),
                cancellationToken).ConfigureAwait(false);

            if (!send.Succeeded)
            {
                _logger.LogWarning("Platform admin reset email failed for {UserId}: {Reason}", user.Id, send.ErrorMessage);

                // Keep the challenge + always return resetToken so the UI can complete reset.
                // In Development (or when email fails), surface the code so local testing is not blocked.
                return new PlatformAdminForgotPasswordResult(
                    true,
                    resetToken,
                    MaskEmail(email),
                    code,
                    $"Could not send email ({send.ErrorMessage}). Use the code shown to reset your password.");
            }

            return new PlatformAdminForgotPasswordResult(
                true,
                resetToken,
                MaskEmail(email),
                _environment.IsDevelopment() ? code : null,
                $"Reset code sent to {MaskEmail(email)}.");
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1)
                return "***";
            return $"{email[0]}***{email[(at - 1)..]}";
        }
    }
}
