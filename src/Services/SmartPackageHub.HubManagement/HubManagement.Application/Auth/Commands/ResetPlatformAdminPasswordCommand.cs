using HubManagement.Application.Auth.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Auth.Commands
{
    public sealed record ResetPlatformAdminPasswordCommand(
        string? ResetToken,
        string? Code,
        string? NewPassword) : IUpdateCommand<PlatformAdminResetPasswordResult>;
}
