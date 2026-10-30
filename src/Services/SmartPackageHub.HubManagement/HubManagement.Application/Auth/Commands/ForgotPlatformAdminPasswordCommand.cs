using HubManagement.Application.Auth.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Auth.Commands
{
    public sealed record ForgotPlatformAdminPasswordCommand(string? UsernameOrEmail)
        : ICreateCommand<PlatformAdminForgotPasswordResult>;
}
