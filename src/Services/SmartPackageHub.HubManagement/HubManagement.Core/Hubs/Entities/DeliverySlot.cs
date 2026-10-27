using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    /// <summary>
    /// Booked door-delivery time window for a package (separate from completed Delivery).
    /// </summary>
    public sealed class DeliverySlot : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; private set; }
        public Guid ResidentId { get; private set; }
        public Guid PackageId { get; private set; }
        public DateTime WindowStart { get; private set; }
        public DateTime WindowEnd { get; private set; }
        public string Status { get; private set; }
        public string Notes { get; private set; }

        private DeliverySlot(
            Guid societyId,
            Guid residentId,
            Guid packageId,
            DateTime windowStart,
            DateTime windowEnd,
            string status,
            string notes)
        {
            SocietyId = societyId;
            ResidentId = residentId;
            PackageId = packageId;
            WindowStart = windowStart;
            WindowEnd = windowEnd;
            Status = status;
            Notes = notes;
        }

        public static DeliverySlot Create(
            Guid societyId,
            Guid residentId,
            Guid packageId,
            DateTime windowStart,
            DateTime windowEnd,
            string status = "booked",
            string? notes = null)
        {
            if (societyId == Guid.Empty)
                throw new ArgumentException("Society is required.", nameof(societyId));
            if (residentId == Guid.Empty)
                throw new ArgumentException("Resident is required.", nameof(residentId));
            if (packageId == Guid.Empty)
                throw new ArgumentException("Package is required.", nameof(packageId));
            if (windowStart >= windowEnd)
                throw new ArgumentException("Window start must be before window end.", nameof(windowStart));

            return new DeliverySlot(
                societyId,
                residentId,
                packageId,
                windowStart,
                windowEnd,
                string.IsNullOrWhiteSpace(status) ? "booked" : status.Trim(),
                notes?.Trim() ?? string.Empty);
        }

        public void Cancel()
        {
            Status = "cancelled";
        }

        /// <summary>Moves a system-default window forward (e.g. next 6–9 while package stays at hub).</summary>
        public void Reschedule(DateTime windowStart, DateTime windowEnd)
        {
            if (windowStart >= windowEnd)
                throw new ArgumentException("Window start must be before window end.", nameof(windowStart));
            WindowStart = windowStart;
            WindowEnd = windowEnd;
        }
    }
}
