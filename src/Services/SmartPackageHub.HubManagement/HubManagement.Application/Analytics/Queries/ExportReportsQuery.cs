using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed record ExportReportsQuery(int Days = 7, Guid? SocietyId = null)
        : Query<ReportsExportResult>;
}
