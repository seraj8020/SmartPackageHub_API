namespace HubManagement.Application.HubResidents.DTOs
{
    /// <summary>
    /// Full resident package payload including hub intake damage note + photo URL when damaged.
    /// </summary>
    public sealed class ResidentPackageDetailDto
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
        public string? PreferredDeliveryMethod { get; set; }
        public bool IsDamaged { get; set; }

        /// <summary>Present only when <see cref="IsDamaged"/> is true.</summary>
        public HubIntakeDamageNoteDto? HubIntakeNote { get; set; }
    }
}
