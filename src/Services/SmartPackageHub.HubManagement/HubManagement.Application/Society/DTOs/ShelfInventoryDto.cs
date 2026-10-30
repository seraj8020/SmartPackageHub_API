namespace HubManagement.Application.Societies.DTOs
{
    /// <summary>Current, non-delivered parcel occupancy for one physical shelf.</summary>
    public sealed class ShelfInventoryDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int PackageCount { get; set; }
        public bool IsFull { get; set; }
        public bool IsColdStorage { get; set; }
        public List<ShelfInventoryPackageDto> Packages { get; set; } = [];
    }

    public sealed class ShelfInventoryPackageDto
    {
        public Guid Id { get; set; }
        public string ResidentName { get; set; } = string.Empty;
        public string PackageNumber { get; set; } = string.Empty;
        public string? Courier { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
