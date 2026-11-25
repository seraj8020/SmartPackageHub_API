namespace HubManagement.Application.HubResidents.DTOs
{
    /// <summary>
    /// Full resident package payload. Hub intake damage and resident return request are separate optional sections.
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

        public AuthorizedCollectorDto? AuthorizedCollector { get; set; }

        /// <summary>Hub slot code (e.g. A-12-3). Null when not yet shelved.</summary>
        public string? ShelfCode { get; set; }
        public string? ShelfTowerOrWing { get; set; }
        public string? ShelfZone { get; set; }
        public string? ShelfAisle { get; set; }
        public int? ShelfTier { get; set; }
        public bool IsColdStorage { get; set; }

        /// <summary>Arrival parcel photo from hub intake (all packages when captured).</summary>
        public HubArrivalPhotoDto? HubArrivalPhoto { get; set; }

        /// <summary>Hub/staff intake damage note + photo. Only when <see cref="IsDamaged"/>.</summary>
        public HubIntakeDamageNoteDto? HubIntakeNote { get; set; }

        /// <summary>Resident return request. Independent of hub intake photos.</summary>
        public ResidentReturnRequestDto? YourReturnRequest { get; set; }
    }
}
