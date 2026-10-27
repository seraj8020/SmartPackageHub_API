using HubManagement.Application.Auth.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Auth.Commands
{
    public sealed record StaffLoginCommand(string? Username, string? Password)
        : ICreateCommand<StaffLoginResult>;
}
