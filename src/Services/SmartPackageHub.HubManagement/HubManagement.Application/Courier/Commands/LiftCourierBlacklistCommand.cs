using HubManagement.Application.Couriers.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Courier.Commands
{
    public sealed record LiftCourierBlacklistCommand(
        Guid BlacklistId,
        string Role,
        IReadOnlyList<Guid> SocietyIds) : IUpdateCommand<CourierCommandResult>;
}