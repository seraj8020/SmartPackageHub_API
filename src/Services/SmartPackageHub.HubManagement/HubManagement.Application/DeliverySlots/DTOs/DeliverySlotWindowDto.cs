
namespace HubManagement.Application.DeliverySlots.DTOs
{
    public sealed class DeliverySlotWindowDto
    {
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }
        public string Label { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int BookedCount { get; set; }
        public int Remaining { get; set; }
        public bool IsFull { get; set; }
    }
}
