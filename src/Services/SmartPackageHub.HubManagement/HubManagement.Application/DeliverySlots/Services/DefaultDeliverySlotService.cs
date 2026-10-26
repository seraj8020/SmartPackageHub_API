using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Repositories;
using HubManagement.Application.DeliverySlots.Helpers;

namespace HubManagement.Application.DeliverySlots.Services
{
    /// <summary>
    /// Keeps a 6?9 system default on hub packages until the resident picks a slot.
    /// </summary>
    internal static class DefaultDeliverySlotService
    {
        public const string DefaultNotes =
            "System default door-delivery window (6?9). Available while the package is at the hub until you pick a slot.";

        public static bool EnsureDefaultForPackage(
            IRepository<DeliverySlot> slots,
            Package package,
            Guid residentId,
            DateTime utcNow,
            IDictionary<Guid, DeliverySlot> activeByPackage)
        {
            ArgumentNullException.ThrowIfNull(slots);
            ArgumentNullException.ThrowIfNull(package);
            ArgumentNullException.ThrowIfNull(activeByPackage);

            if (activeByPackage.TryGetValue(package.Id, out var active))
            {
                // Resident already chose a slot ? do not touch it.
                if (!DeliverySlotWindows.IsSystemDefault(active.Status))
                    return false;

                // Default expired (day passed) while package still at hub ? roll to next 6?9.
                if (active.WindowEnd <= utcNow)
                {
                    var (nextStart, nextEnd) = DeliverySlotWindows.NextDefaultWindow(utcNow);
                    active.Reschedule(nextStart, nextEnd);
                    return true;
                }

                return false;
            }

            var (windowStart, windowEnd) = DeliverySlotWindows.NextDefaultWindow(utcNow);
            var created = DeliverySlot.Create(
                package.SocietyId,
                residentId,
                package.Id,
                windowStart,
                windowEnd,
                DeliverySlotWindows.StatusDefault,
                DefaultNotes);
            slots.Insert(created);
            activeByPackage[package.Id] = created;
            return true;
        }
    }
}
