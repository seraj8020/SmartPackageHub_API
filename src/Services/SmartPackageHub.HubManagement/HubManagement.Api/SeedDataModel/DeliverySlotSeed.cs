namespace HubManagement.Api.SeedDataModel
{
    internal sealed class DeliverySlotSeed
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public Guid ResidentId { get; set; }
        public Guid PackageId { get; set; }
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }
        public string Status { get; set; } = "booked";
        public string Notes { get; set; } = string.Empty;
    }
}
