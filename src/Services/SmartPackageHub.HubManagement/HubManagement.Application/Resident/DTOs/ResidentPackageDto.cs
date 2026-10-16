namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed class ResidentPackageDto
    {
        public Guid Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ReceivedAt { get; set; }
        public string? Courier { get; set; }
        public string? Category { get; set; }
        public double Weight { get; set; }
        public string PkgNumber { get; set; } = string.Empty;
        public string TowerWing { get; set; } = string.Empty;
        public string FlatNumber { get; set; } = string.Empty;
    }
}
