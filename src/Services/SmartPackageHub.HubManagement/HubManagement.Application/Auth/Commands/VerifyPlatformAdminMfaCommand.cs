using HubManagement.Application.Auth.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Auth.Commands
{
    public sealed record VerifyPlatformAdminMfaCommand(string? MfaToken, string? Code)
        : ICreateCommand<PlatformAdminLoginResult>;
}
