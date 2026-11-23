using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed record GetTopScannersQuery(Guid? SocietyId = null, int Days = 30, int Take = 5)
        : Query<TopScannersDashboardDto>;
}
