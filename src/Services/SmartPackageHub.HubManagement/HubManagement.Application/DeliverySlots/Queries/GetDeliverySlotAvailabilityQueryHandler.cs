using AutoMapper;
using HubManagement.Application.DeliverySlots.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;
using HubManagement.Application.DeliverySlots.Helpers;

namespace HubManagement.Application.DeliverySlots.Queries
{
    public sealed class GetDeliverySlotAvailabilityQueryHandler
        : QueryHandler<GetDeliverySlotAvailabilityQuery, DeliverySlotAvailabilityDto?>
    {
        private readonly IRepository<DeliverySlot> _slots;

        public GetDeliverySlotAvailabilityQueryHandler(
            IMapper mapper,
            IRepository<DeliverySlot> slots) : base(mapper)
        {
            _slots = slots;
        }

        protected override Task<DeliverySlotAvailabilityDto?> HandleAsync(GetDeliverySlotAvailabilityQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var dayStart = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var dayEnd = dayStart.AddDays(1);
            var bookings = _slots
                .FetchMulti(slot =>
                    slot.SocietyId == request.SocietyId &&
                    slot.Status != DeliverySlotWindows.StatusCancelled &&
                    slot.WindowStart >= dayStart &&
                    slot.WindowStart < dayEnd)
                .ToList();

            var windows = DeliverySlotWindows.DayWindows.Select(window =>
            {
                var windowStart = dayStart.Add(window.Start);
                var windowEnd = dayStart.Add(window.End);
                var booked = bookings.Count(slot =>
                    slot.WindowStart == windowStart && slot.WindowEnd == windowEnd);
                var remaining = Math.Max(0, DeliverySlotWindows.CapacityPerWindow - booked);
                return new DeliverySlotWindowDto
                {
                    WindowStart = windowStart,
                    WindowEnd = windowEnd,
                    Label = window.Label,
                    Capacity = DeliverySlotWindows.CapacityPerWindow,
                    BookedCount = booked,
                    Remaining = remaining,
                    IsFull = remaining == 0,
                };
            }).ToList();

            return Task.FromResult<DeliverySlotAvailabilityDto?>(new DeliverySlotAvailabilityDto
            {
                Date = request.Date,
                Windows = windows,
            });
        }
    }
}
