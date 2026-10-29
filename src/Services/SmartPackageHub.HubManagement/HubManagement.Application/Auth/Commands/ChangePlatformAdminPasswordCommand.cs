using HubManagement.Application.Auth.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Auth.Commands
{
    public sealed record ChangePlatformAdminPasswordCommand(
        string? AccessToken,
        string? CurrentPassword,
        string? NewPassword) : IUpdateCommand<PlatformAdminPasswordChangeResult>;
}
