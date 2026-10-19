namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed class DeliveryHistoryDto
    {
        public Guid Id { get; set; }
        public Guid PackageId { get; set; }
        public DateTime DeliveredAt { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PkgNumber { get; set; } = string.Empty;
        public string? Courier { get; set; }
        public string? Category { get; set; }
    }
}
