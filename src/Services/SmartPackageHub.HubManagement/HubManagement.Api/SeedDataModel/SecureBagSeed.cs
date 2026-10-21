namespace HubManagement.Api.SeedDataModel
{
    internal sealed class SecureBagSeed
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public string BagNumber { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? QrCode { get; set; }
        public string? SealNumber { get; set; }
        public string? CapacityOrSize { get; set; }
        public string Status { get; set; } = "Assigned";
    }
}
