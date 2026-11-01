using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed record CancelResidentVacationHoldCommand(Guid ResidentId) : CreateCommand;
}
