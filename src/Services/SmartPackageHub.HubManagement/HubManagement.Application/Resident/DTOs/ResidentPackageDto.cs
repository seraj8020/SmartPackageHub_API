namespace HubManagement.Application.HubResidents.DTOs
{
    /// <summary>
    /// Lean inbox / slot list payload for the resident app.
    /// </summary>
    public sealed class ResidentPackageDto
    {
        public Guid Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public string? Courier { get; set; }
        public string? Category { get; set; }
        public double Weight { get; set; }
        public string FlatNumber { get; set; } = string.Empty;
        public string? PkgNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsCod { get; set; }
        public decimal? CodAmount { get; set; }
        public string CodApprovalStatus { get; set; } = "None";
        public string? DeliverySlotStatus { get; set; }
        public string? DeliverySlotLabel { get; set; }
        public DateTime? DeliverySlotWindowStart { get; set; }
        public DateTime? DeliverySlotWindowEnd { get; set; }
    }
}
