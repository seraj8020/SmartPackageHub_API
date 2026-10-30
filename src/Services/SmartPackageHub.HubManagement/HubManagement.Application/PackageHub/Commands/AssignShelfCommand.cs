using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed record AssignShelfCommand(Guid PackageId, string? ShelfCode, bool IsColdStorage = false)
        : ICreateCommand<AssignShelfResult>;

    public sealed class AssignShelfResult
    {
        public bool Found { get; init; }
        public bool Succeeded { get; init; }
        public bool Conflict { get; init; }
        public string? Message { get; init; }
        public ConfirmIntakeResponse? Receipt { get; init; }
    }
}
