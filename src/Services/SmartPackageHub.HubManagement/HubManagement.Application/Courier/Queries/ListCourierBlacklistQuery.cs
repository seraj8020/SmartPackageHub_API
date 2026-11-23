using HubManagement.Application.Couriers.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Courier.Queries
{
    public sealed record ListCourierBlacklistQuery(
        string Role,
        IReadOnlyList<Guid> SocietyIds,
        Guid? SocietyId) : Query<CourierListResult<CourierBlacklistDto>>;
}