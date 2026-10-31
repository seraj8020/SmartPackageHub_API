using HubManagement.Application.Intake.DTOs;

namespace HubManagement.Application.PackageHub.DTOs
{
    public sealed class AssignShelfResult
    {
        public bool Found { get; init; }
        public bool Succeeded { get; init; }
        public bool Conflict { get; init; }
        public string? Message { get; init; }
        public ConfirmIntakeResponse? Receipt { get; init; }
    }
}
