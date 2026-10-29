using HubManagement.Application.Auth.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Auth.Commands
{
    public sealed record PlatformAdminLoginCommand(string? Username, string? Password)
        : ICreateCommand<PlatformAdminLoginResult>;
}
