using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed record GetDashboardMetricPackagesQuery(
        Guid? SocietyId = null,
        DateOnly? Day = null,
        string Metric = "received",
        int Page = 1,
        int PageSize = 20) : Query<DashboardMetricPackagesPageDto>;
}
