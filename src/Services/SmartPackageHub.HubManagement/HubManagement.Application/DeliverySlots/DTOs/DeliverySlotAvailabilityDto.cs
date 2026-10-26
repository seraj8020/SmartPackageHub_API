
namespace HubManagement.Application.DeliverySlots.DTOs
{
    public sealed class DeliverySlotAvailabilityDto
    {
        public DateOnly Date { get; set; }
        public List<DeliverySlotWindowDto> Windows { get; set; } = new();
    }
}
