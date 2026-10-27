using HubManagement.Application.DeliverySlots.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.DeliverySlots.Queries
{
    public sealed record GetDeliverySlotAvailabilityQuery(Guid SocietyId, DateOnly Date)
        : Query<DeliverySlotAvailabilityDto?>;
}
