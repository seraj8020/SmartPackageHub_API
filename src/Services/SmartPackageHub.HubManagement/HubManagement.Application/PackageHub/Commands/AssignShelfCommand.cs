using HubManagement.Application.Intake.DTOs;
using HubManagement.Application.PackageHub.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed record AssignShelfCommand(Guid PackageId, string? ShelfCode, bool IsColdStorage = false)
        : ICreateCommand<AssignShelfResult>;
}
