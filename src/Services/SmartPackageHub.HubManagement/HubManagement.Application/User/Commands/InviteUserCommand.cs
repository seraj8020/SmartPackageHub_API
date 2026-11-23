using HubManagement.Application.HubUsers.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubUsers.Commands
{
    public sealed record InviteUserCommand(
        string CallerRole,
        IReadOnlyList<Guid> CallerSocietyIds,
        string Username,
        string Email,
        string PhoneNumber,
        string? Role,
        string Password,
        Guid? SocietyId) : ICreateCommand<UserCommandResult>;
}