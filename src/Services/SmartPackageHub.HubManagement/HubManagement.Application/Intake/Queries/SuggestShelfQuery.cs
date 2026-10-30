using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Intake.Queries
{
    public sealed record SuggestShelfQuery(Guid? SocietyId) : Query<ShelfResponse?>;
}
