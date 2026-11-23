using HubManagement.Application.HubUsers.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubUsers.Commands
{
    public sealed record UpdateResidentUserCommand(
        Guid UserId,
        string CallerRole,
        IReadOnlyList<Guid> CallerSocietyIds,
        string Username,
        string PhoneNumber,
        string TowerWing,
        string UnitNumber,
        string Status,
        Guid? SocietyId) : IUpdateCommand<UserCommandResult>;
}