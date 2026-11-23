using HubManagement.Application.Couriers.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Courier.Commands
{
    public sealed record UpdateCourierAgentCommand(
        Guid AgentId,
        string Role,
        IReadOnlyList<Guid> SocietyIds,
        UpdateCourierAgentRequest Body) : IUpdateCommand<CourierCommandResult>;
}