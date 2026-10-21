using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Intake.Queries
{
    public sealed record MatchResidentsQuery(string? Tower, string? Flat, string? Name) : Query<MatchResponse>;
}
