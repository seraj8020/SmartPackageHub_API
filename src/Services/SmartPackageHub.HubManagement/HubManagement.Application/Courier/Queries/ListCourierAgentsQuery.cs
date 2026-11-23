using HubManagement.Application.Couriers.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Courier.Queries
{
    public sealed record ListCourierAgentsQuery(
        string Role,
        IReadOnlyList<Guid> SocietyIds,
        Guid? SocietyId) : Query<CourierListResult<CourierAgentDto>>;
}