
namespace HubManagement.Application.Societies.DTOs
{
    public sealed class ShelfInventoryPackageDto
    {
        public Guid Id { get; set; }
        public string ResidentName { get; set; } = string.Empty;
        public string PackageNumber { get; set; } = string.Empty;
        public string? Courier { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
