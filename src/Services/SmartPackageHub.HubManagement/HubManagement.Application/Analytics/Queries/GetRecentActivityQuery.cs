using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    /// <param name="Day">UTC calendar day — when set, returns that day's activity (whole day), not a tiny rolling window.</param>
    public sealed record GetRecentActivityQuery(
        Guid? SocietyId = null,
        int Take = 20,
        DateOnly? Day = null) : Query<IReadOnlyList<RecentActivityItemDto>>;
}
