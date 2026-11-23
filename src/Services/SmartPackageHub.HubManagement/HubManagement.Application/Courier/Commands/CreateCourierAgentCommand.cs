using HubManagement.Application.Couriers.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Courier.Commands
{
    public sealed record CreateCourierAgentCommand(
        string Role,
        IReadOnlyList<Guid> SocietyIds,
        CreateCourierAgentRequest Body) : ICreateCommand<CourierCommandResult>;
}