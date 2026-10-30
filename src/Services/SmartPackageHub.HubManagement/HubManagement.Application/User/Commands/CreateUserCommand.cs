using HubManagement.Application.HubUsers.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubUsers.Commands
{
    public sealed record CreateUserCommand(
        string Username,
        string Email,
        string? Role,
        bool IsAdmin,
        string? Status,
        string? Password = null,
        string? PhoneNumber = null) : ICreateCommand<UserCommandResult>;

    
}
