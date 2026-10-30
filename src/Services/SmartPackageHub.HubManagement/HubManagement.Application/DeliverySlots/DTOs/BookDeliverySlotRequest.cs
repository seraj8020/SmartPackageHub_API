namespace HubManagement.Application.DeliverySlots.DTOs
{
    public sealed class BookDeliverySlotRequest
    {
        public Guid SocietyId { get; set; }
        public Guid ResidentId { get; set; }
        public Guid PackageId { get; set; }
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }
        public string? Notes { get; set; }
    }
}
