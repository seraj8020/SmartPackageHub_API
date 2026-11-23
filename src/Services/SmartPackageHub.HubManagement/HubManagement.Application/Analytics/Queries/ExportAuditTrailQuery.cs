using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    /// <summary>
    /// CSV export of package custody history for reports.
    /// Same filters as audit search; optional <paramref name="PackageId"/> exports one package only.
    /// </summary>
    public sealed record ExportAuditTrailQuery(
        Guid? SocietyId,
        string? Q = null,
        string? Courier = null,
        DateOnly? FromDate = null,
        DateOnly? ToDate = null,
        Guid? PackageId = null) : Query<ReportsExportResult>;
}
