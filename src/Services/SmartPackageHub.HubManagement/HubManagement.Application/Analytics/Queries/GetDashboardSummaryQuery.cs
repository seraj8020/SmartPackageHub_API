using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed record GetDashboardSummaryQuery(Guid? SocietyId = null)
        : Query<DashboardSummaryDto>;
}
