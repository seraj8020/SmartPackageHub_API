namespace HubManagement.Api.SeedDataModel
{
    internal sealed class PackageSeed
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public Guid? ResidentId { get; set; }
        public Guid? ShelfId { get; set; }
        public Guid? SecureBagId { get; set; }
        public Guid? DigitalSignOffReceiptId { get; set; }
        public Guid StaffUserId { get; set; }
        public string PkgNumber { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public double Weight { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public string? Category { get; set; }
        public string CourierPartner { get; set; } = string.Empty;
        public string CourierAwb { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string TowerWing { get; set; } = string.Empty;
        public string FlatNumber { get; set; } = string.Empty;
        public string Status { get; set; } = "Received";
        public bool IsColdStorage { get; set; }
        public bool IsCod { get; set; }
        public decimal? CodAmount { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
