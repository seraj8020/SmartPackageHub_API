using HubManagement.Application.Couriers.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Courier.Commands
{
    public sealed record BlacklistCourierAgentCommand(
        Guid AgentId,
        string Role,
        IReadOnlyList<Guid> SocietyIds,
        BlacklistCourierRequest Body) : IUpdateCommand<CourierCommandResult>;
}