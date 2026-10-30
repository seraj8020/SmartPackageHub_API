using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Intake.Commands
{
    public sealed record DispatchIntakeNotificationCommand(Guid IntakeId) : IUpdateCommand<bool>;
    
}
