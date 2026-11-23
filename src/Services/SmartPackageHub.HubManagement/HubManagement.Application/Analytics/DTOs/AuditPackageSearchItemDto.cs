namespace HubManagement.Application.Analytics.DTOs
{
    public sealed class AuditPackageSearchItemDto
    {
        public Guid Id { get; set; }
        public string PackageNumber { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public string? CourierAwb { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string? Courier { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ReceivedAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }
        public string? TowerWing { get; set; }
        public string FlatNumber { get; set; } = string.Empty;
        public Guid SocietyId { get; set; }
        public string? SocietyName { get; set; }
    }
}
