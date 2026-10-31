
namespace HubManagement.Application.DeliverySlots.DTOs
{
    public sealed class DeliverySlotAvailabilityDto
    {
        public DateOnly Date { get; set; }
        public IReadOnlyList<DeliverySlotWindowDto> Windows { get; init; } = Array.Empty<DeliverySlotWindowDto>();
    }
}
