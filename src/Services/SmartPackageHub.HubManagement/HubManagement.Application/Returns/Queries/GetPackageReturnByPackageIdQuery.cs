using HubManagement.Application.Returns.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Returns.Queries
{
    public sealed record GetPackageReturnByPackageIdQuery(Guid PackageId, bool PreferOpen = true)
        : Query<PackageReturnDto?>;
}
