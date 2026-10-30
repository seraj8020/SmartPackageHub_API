using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed record RespondCodCommand(Guid PackageId, bool Approve) : ICreateCommand<CodResponseResult>;

    public sealed class CodResponseResult
    {
        public bool Found { get; init; }
        public bool Succeeded { get; init; }
        public bool Conflict { get; init; }
        public string? Message { get; init; }
        public string? PickupOtp { get; init; }
        public string? Status { get; init; }
        public string? CodApprovalStatus { get; init; }
    }
}
