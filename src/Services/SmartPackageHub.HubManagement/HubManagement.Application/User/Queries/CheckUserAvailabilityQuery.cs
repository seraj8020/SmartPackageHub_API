using HubManagement.Application.HubUsers.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.HubUsers.Queries
{
    public sealed record CheckUserAvailabilityQuery(
        string CallerRole,
        string Field,
        string Value,
        Guid? ExcludeUserId) : Query<UserAvailabilityQueryResult>;
}