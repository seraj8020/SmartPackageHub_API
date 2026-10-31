using HubManagement.Application.Auth.DTOs;
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
    public sealed class ForgotPlatformAdminUsernameCommandHandler
        : CreateCommandHandler<ForgotPlatformAdminUsernameCommand, PlatformAdminForgotUsernameResult>
    {
        private static readonly Action<ILogger, Guid, string, Exception?> LogUsernameEmailFailed =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Warning,
                new EventId(1, nameof(ForgotPlatformAdminUsernameCommandHandler)),
                "Platform admin username reminder email failed for {UserId}: {Reason}");

        private readonly IRepository<User> _users;
        private readonly IEmailSender _emailSender;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<ForgotPlatformAdminUsernameCommandHandler> _logger;

        public ForgotPlatformAdminUsernameCommandHandler(
            IRepository<User> users,
            IEmailSender emailSender,
            IHostEnvironment environment,
            ILogger<ForgotPlatformAdminUsernameCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _emailSender = emailSender;
            _environment = environment;
            _logger = logger;
        }

        public override async Task<PlatformAdminForgotUsernameResult> Handle(
            ForgotPlatformAdminUsernameCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            const string genericMessage =
                "If a platform admin account matches that email, the username was sent.";

            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@', StringComparison.Ordinal))
            {
                return new PlatformAdminForgotUsernameResult(
                    false, null, null, "Enter the email registered on your platform admin account.");
            }

            var email = request.Email.Trim();
            var user = _users.GetAll().AsEnumerable()
                .FirstOrDefault(item =>
                    item.Role.Type is (RoleType.Admin or RoleType.PlatformAdmin) &&
                    string.Equals(item.Status, "Active", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.ContactInfo?.Email, email, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                if (_environment.IsDevelopment())
                {
                    return new PlatformAdminForgotUsernameResult(
                        false, null, null, "No active admin matched that email.");
                }

                return new PlatformAdminForgotUsernameResult(true, null, null, genericMessage);
            }

            var send = await _emailSender.SendAsync(
                new EmailSendRequest(
                    email,
                    "Smart Package Hub — Platform Admin username reminder",
                    $"Your Smart Package Hub platform admin username is: {user.Username}\n\nIf you did not request this, you can ignore this email.",
                    $"platform-username:{user.Id:N}"),
                cancellationToken).ConfigureAwait(false);

            if (!send.Succeeded)
            {
                LogUsernameEmailFailed(_logger, user.Id, send.ErrorMessage ?? string.Empty, null);

                if (!_environment.IsDevelopment())
                    return new PlatformAdminForgotUsernameResult(true, null, null, genericMessage);

                return new PlatformAdminForgotUsernameResult(
                    true,
                    user.Username,
                    MaskEmail(email),
                    $"Email failed: {send.ErrorMessage}. Development username: {user.Username}");
            }

            return new PlatformAdminForgotUsernameResult(
                true,
                _environment.IsDevelopment() ? user.Username : null,
                MaskEmail(email),
                $"Username reminder sent to {MaskEmail(email)}.");
        }

        private static string MaskEmail(string value)
        {
            var at = value.IndexOf('@', StringComparison.Ordinal);
            if (at <= 1)
                return "***";
            return $"{value[0]}***{value[(at - 1)..]}";
        }
    }
}
