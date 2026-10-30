namespace HubManagement.Api.SeedDataModel
{
    internal sealed class DeliverySeed
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public Guid PackageId { get; set; }
        public Guid HandedByUserId { get; set; }
        public Guid ResidentId { get; set; }
        public string Method { get; set; } = "CounterPickup";
        public DateTime DeliveredAt { get; set; }
        public string Status { get; set; } = "Completed";
        public string Notes { get; set; } = string.Empty;
    }
}
