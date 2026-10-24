using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed record SaveResidentVacationHoldCommand(Guid societyId, Guid residentId, DateTime startDate, DateTime endDate, bool isActive) : CreateCommand;
}
