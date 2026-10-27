
namespace HubManagement.Application.DeliverySlots.DTOs
{
    public sealed class BookDeliverySlotResult
    {
        public bool Found { get; set; }
        public bool Conflict { get; set; }
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public DeliverySlotDto? Slot { get; set; }
    }
}
