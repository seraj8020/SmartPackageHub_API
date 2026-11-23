using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed record GetPackageAuditTimelineQuery(Guid PackageId, Guid? SocietyId = null)
        : Query<PackageAuditTimelineDto?>;
}
