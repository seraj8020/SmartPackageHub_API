using HubManagement.Application.HubResidents.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed record GetPackageAuthorizedCollectorQuery(Guid ResidentId, Guid PackageId)
        : Query<AuthorizedCollectorDto?>;
}
