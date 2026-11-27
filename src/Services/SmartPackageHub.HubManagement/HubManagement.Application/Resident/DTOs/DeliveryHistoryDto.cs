namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed class DeliveryHistoryDto
    {
        public Guid Id { get; set; }
        public Guid PackageId { get; set; }
        public DateTime DeliveredAt { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        /// <summary>Delivery attempt status (e.g. Completed) — not the package lifecycle.</summary>
        public string Status { get; set; } = string.Empty;
        /// <summary>Package lifecycle (Delivered, ReturnRequested, …) used for return eligibility.</summary>
        public string? PackageStatus { get; set; }
        public string PkgNumber { get; set; } = string.Empty;
        /// <summary>SPH tracking number for the package.</summary>
        public string? TrackingNumber { get; set; }
        /// <summary>Courier AWB / airway bill from the label.</summary>
        public string? CourierAwb { get; set; }
        public string? Courier { get; set; }
        public string? Category { get; set; }

        /// <summary>True when hub intake recorded damage. Note/photo load via package detail only.</summary>
        public bool IsDamaged { get; set; }

        /// <summary>Open or latest package return status when a return exists for this package.</summary>
        public string? ReturnStatus { get; set; }

        /// <summary>Stored return reason enum name (e.g. DamagedInside, Other).</summary>
        public string? ReturnReason { get; set; }

        /// <summary>Optional note from the resident return request (required when reason is Other).</summary>
        public string? ReturnNote { get; set; }

        /// <summary>When the resident submitted the return request.</summary>
        public DateTime? ReturnRequestedAtUtc { get; set; }
    }
}
