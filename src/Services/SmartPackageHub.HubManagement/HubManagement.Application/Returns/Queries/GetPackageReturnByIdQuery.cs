using HubManagement.Application.Returns.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Returns.Queries
{
    public sealed record GetPackageReturnByIdQuery(Guid ReturnId) : Query<PackageReturnDto?>;
}
