using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Intake.Commands
{
    public sealed record BatchIntakeCommand(BatchIntakeRequest Request)
        : ICreateCommand<IntakeStatusResult<BatchIntakeResponse>>;
}
