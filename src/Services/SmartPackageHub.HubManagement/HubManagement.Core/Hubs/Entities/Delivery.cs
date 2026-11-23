using SmartPackageHub.Domains.Abstractions.Entities;
using HubManagement.Core.Hubs.ValueObjects;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class Delivery : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; private set; }
        public Guid PackageId { get; private set; }
        public Guid HandedByUserId { get; private set; }
        public Guid ResidentId { get; private set; }
        public Guid? OtpLogId { get; private set; }
        public Guid? BatchId { get; private set; }
        public DeliveryStatus Status { get; private set; }
        public string Method { get; private set; } // CounterPickup, DoorDelivery
        public DateTime DeliveredAt { get; private set; }
        public string Notes { get; private set; }

        /// <summary>
        /// Set when the attempt did not complete (e.g. ResidentUnavailable for same-shelf re-shelve).
        /// Stored as string column; null on successful deliveries.
        /// </summary>
        public DeliveryAttemptOutcome? FailureReason { get; private set; }

        public Delivery(
            Guid societyId,
            Guid packageId,
            Guid handedByUserId,
            Guid residentId,
            Guid? otpLogId,
            Guid? batchId,
            string method,
            DateTime deliveredAt,
            string notes)
        {
            SocietyId = societyId;
            PackageId = packageId;
            HandedByUserId = handedByUserId;
            ResidentId = residentId;
            OtpLogId = otpLogId;
            BatchId = batchId;
            Method = method;
            DeliveredAt = deliveredAt;
            Notes = notes;
            Status = DeliveryStatus.Attempted;
            FailureReason = null;
        }

        public static Delivery Create(
            Guid societyId,
            Guid packageId,
            Guid staffId,
            Guid residentId,
            Guid? otpLogId,
            Guid? batchId,
            string method,
            DateTime deliveredAt,
            string notes)
        {
            if (packageId == Guid.Empty)
                throw new ArgumentNullException(nameof(packageId));
            if (residentId == Guid.Empty)
                throw new ArgumentNullException(nameof(residentId));
            return new Delivery(societyId, packageId, staffId, residentId, otpLogId, batchId, method, deliveredAt, notes);
        }

        public void CompleteDelivery()
        {
            Status = DeliveryStatus.Completed;
        }

        public void RejectDelivery()
        {
            Status = DeliveryStatus.Rejected;
        }

        /// <summary>
        /// Marks a failed door-delivery attempt (resident not available, etc.) for re-shelve flows.
        /// </summary>
        public void MarkAttemptFailed(DeliveryAttemptOutcome outcome, string? notes = null)
        {
            ArgumentNullException.ThrowIfNull(outcome);
            if (Status.Equals(DeliveryStatus.Completed))
                throw new InvalidOperationException("Completed deliveries cannot be marked as failed attempts.");

            FailureReason = outcome;
            Status = DeliveryStatus.Rejected;
            if (!string.IsNullOrWhiteSpace(notes))
                Notes = notes.Trim();
        }
    }
}
