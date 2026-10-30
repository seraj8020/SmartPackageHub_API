using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Intake.Commands
{
    public sealed record ConfirmIntakeCommand(ConfirmIntakeRequest Request) : ICreateCommand<IntakeStatusResult<ConfirmIntakeResponse>>;
}
