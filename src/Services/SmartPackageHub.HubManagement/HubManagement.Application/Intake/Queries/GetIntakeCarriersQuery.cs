using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Intake.Queries
{
    public sealed record GetIntakeCarriersQuery(Guid? SocietyId) : Query<List<string>>;
}
