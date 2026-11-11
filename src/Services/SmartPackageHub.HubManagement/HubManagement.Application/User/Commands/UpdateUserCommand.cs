using HubManagement.Application.HubUsers.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubUsers.Commands
{
    public sealed record UpdateUserCommand(
        Guid UserId,
        string? Username,
        string? Email,
        string? Role,
        bool? IsAdmin,
        string? Status,
        string? PhoneNumber = null,
        IReadOnlyList<Guid>? SocietyIds = null) : IUpdateCommand<UserCommandResult>;
}
