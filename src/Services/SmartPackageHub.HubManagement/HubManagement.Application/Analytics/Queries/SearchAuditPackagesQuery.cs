using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed record SearchAuditPackagesQuery(
        Guid? SocietyId,
        string? Q = null,
        string? Courier = null,
        DateOnly? FromDate = null,
        DateOnly? ToDate = null,
        int Page = 1,
        int PageSize = 20) : Query<AuditPackageSearchPageDto>;
}
