using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed record GetReportsDailySeriesQuery(int Days = 7, Guid? SocietyId = null)
        : Query<IReadOnlyList<ReportsDailyPointDto>>;
}
