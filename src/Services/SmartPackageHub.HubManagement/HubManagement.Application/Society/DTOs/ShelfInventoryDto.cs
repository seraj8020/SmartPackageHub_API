namespace HubManagement.Application.Societies.DTOs
{
    /// <summary>Current Shelved occupancy for one physical shelf (excludes Dispatched / Delivered).</summary>
    public sealed class ShelfInventoryDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int PackageCount { get; set; }
        public bool IsFull { get; set; }
        public bool IsColdStorage { get; set; }
        public IReadOnlyList<ShelfInventoryPackageDto> Packages { get; init; } = Array.Empty<ShelfInventoryPackageDto>();
    }
    
}
