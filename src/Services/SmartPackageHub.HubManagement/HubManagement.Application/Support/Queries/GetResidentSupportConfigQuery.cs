using HubManagement.Application.Support.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Support.Queries
{
    public sealed record GetResidentSupportConfigQuery : Query<ResidentSupportConfigDto>;
}
