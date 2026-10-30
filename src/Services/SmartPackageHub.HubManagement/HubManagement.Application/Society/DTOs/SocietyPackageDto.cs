namespace HubManagement.Application.Societies.DTOs
{
    /// <summary>
    /// Lean list payload for staff Receive / Handover / Deliver screens.
    /// </summary>
    public sealed class SocietyPackageDto
    {
        public Guid Id { get; set; }
        public Guid? ResidentId { get; set; }
        public Guid StaffUserId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? Courier { get; set; }
        public string? Category { get; set; }
        public double Weight { get; set; }
        public string? TowerWing { get; set; }
        public string FlatNumber { get; set; } = string.Empty;
        public string? ShelfCode { get; set; }
        public int? ShelfCapacity { get; set; }
        public bool IsColdStorage { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsCod { get; set; }
        public decimal? CodAmount { get; set; }
        public string CodApprovalStatus { get; set; } = "None";
        public string? DeliverySlotStatus { get; set; }
        public string? DeliverySlotLabel { get; set; }
    }
}
