namespace HubManagement.Application.PackageHub.DTOs
{
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
