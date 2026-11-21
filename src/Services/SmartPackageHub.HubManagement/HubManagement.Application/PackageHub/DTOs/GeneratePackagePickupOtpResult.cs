namespace HubManagement.Application.PackageHub.DTOs
{
    public sealed class GeneratePackagePickupOtpResult
    {
        public bool Found { get; init; }
        public bool Succeeded { get; init; }
        public bool Conflict { get; init; }
        public string? Message { get; init; }
        public string? PickupOtp { get; init; }
        public bool SmsQueued { get; init; }
    }
}
