using HubManagement.Application.DeliverySlots.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.DeliverySlots.Commands
{
    public sealed record BookDeliverySlotCommand(BookDeliverySlotRequest Request)
        : ICreateCommand<BookDeliverySlotResult>;
}
