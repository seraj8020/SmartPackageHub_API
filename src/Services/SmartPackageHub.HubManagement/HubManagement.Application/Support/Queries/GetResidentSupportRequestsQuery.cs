using HubManagement.Application.Support.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Support.Queries
{
    public sealed record GetResidentSupportRequestsQuery(Guid ResidentId)
        : Query<List<SupportRequestDto>?>;
}
