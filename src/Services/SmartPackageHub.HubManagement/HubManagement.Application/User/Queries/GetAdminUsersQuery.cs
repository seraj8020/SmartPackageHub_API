using HubManagement.Application.HubUsers.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.HubUsers.Queries
{
    public sealed record GetAdminUsersQuery(
        string CallerRole,
        IReadOnlyList<Guid> CallerSocietyIds,
        bool ResidentsOnly = false) : Query<GetAdminUsersQueryResult>;
}