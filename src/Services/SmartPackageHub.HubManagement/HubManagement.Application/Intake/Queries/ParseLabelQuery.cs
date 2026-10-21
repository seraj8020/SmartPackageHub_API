using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Intake.Queries
{
    public sealed record ParseLabelQuery(string? Text) : Query<ParsedLabelDto?>;
}
