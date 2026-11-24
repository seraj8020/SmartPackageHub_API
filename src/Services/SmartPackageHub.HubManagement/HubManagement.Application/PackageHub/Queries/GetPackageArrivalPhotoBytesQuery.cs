using HubManagement.Application.HubResidents.Queries;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.PackageHub.Queries
{
    /// <summary>Staff-facing arrival photo stream by package id (no resident ownership check).</summary>
    public sealed record GetPackageArrivalPhotoBytesQuery(
        Guid PackageId,
        IReadOnlyCollection<Guid> AllowedSocietyIds)
        : Query<ResidentPhotoBytes?>;
}
