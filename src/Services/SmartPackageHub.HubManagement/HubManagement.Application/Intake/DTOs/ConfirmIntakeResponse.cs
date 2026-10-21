namespace HubManagement.Application.Intake.DTOs
{
    public sealed class ConfirmIntakeResponse
    {
        public Guid IntakeId { get; set; }
        public Guid PackageId { get; set; }
        public string PkgNumber { get; set; } = string.Empty;
        public string Awb { get; set; } = string.Empty;
        public string? Courier { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public string? PickupOtp { get; set; }
        public string PickupQrPayload { get; set; } = string.Empty;
        public string NotificationStatus { get; set; } = "Queued";
        public string MatchStatus { get; set; } = "missing";
        public bool PhotoStored { get; set; }
        public ResidentMatchDto? Resident { get; set; }
    }
}
