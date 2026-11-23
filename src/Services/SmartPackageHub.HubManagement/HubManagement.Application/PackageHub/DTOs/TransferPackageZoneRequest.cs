namespace HubManagement.Application.PackageHub.DTOs
{
    public sealed class TransferPackageZoneRequest
    {
        /// <summary>Destination zone label, e.g. "Tower B cage".</summary>
        public string ToZone { get; set; } = string.Empty;

        /// <summary>Optional shelf/bin at the destination (e.g. H-04-2).</summary>
        public string? LocationLabel { get; set; }

        /// <summary>Optional override for source zone; defaults to last hop / Hub.</summary>
        public string? FromZone { get; set; }

        public string? Notes { get; set; }
    }
}
