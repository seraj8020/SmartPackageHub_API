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
        public string Status { get; set; } = "Received";
        public bool IsCod { get; set; }
        public decimal? CodAmount { get; set; }
        public string CodApprovalStatus { get; set; } = "None";
        public string? PickupOtp { get; set; }
        public string PickupQrPayload { get; set; } = string.Empty;
        public string NotificationStatus { get; set; } = "Queued";
        public string MatchStatus { get; set; } = "missing";
        public bool PhotoStored { get; set; }
        public ResidentMatchDto? Resident { get; set; }
    }
}
