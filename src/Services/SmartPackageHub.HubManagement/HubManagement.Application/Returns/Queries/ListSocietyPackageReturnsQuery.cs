using HubManagement.Application.Returns.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Returns.Queries
{
    public sealed record ListSocietyPackageReturnsQuery(
        Guid SocietyId,
        bool OpenOnly = true)
        : Query<IReadOnlyList<PackageReturnDto>>;
}
