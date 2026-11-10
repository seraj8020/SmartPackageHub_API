using HubManagement.Application.HubResidents.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed record GetResidentVacationHoldQuery(Guid ResidentId) : Query<GetResidentVacationHoldResult>;
}
