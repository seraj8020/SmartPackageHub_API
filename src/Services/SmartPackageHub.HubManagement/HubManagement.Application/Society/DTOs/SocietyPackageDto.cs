namespace HubManagement.Application.Societies.DTOs
{
    public sealed class SocietyPackageDto
    {
        public Guid Id { get; set; }
        public Guid? ResidentId { get; set; }
        public Guid StaffUserId { get; set; }
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
