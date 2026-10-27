using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Intake.Queries
{
    public sealed record SearchResidentsQuery(string? Query, Guid? SocietyId = null) : Query<List<ResidentMatchDto>>;
}
