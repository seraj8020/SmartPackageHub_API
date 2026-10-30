using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed record GetRecentActivityQuery(Guid? SocietyId = null, int Take = 20)
        : Query<IReadOnlyList<RecentActivityItemDto>>;
}
