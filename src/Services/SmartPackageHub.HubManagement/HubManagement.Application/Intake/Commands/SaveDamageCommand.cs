using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Intake.Commands
{
    public sealed record SaveDamageCommand(Guid IntakeId, DamageIntakeRequest Request) : IUpdateCommand<DamageSaveResult>;

    
}
