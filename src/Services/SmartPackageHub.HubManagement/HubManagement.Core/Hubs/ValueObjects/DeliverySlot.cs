using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class DeliverySlot : ValueObject
    {
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public string Status { get; private set; } // booked, at-the-counter
        private DeliverySlot(DateTime startTime, DateTime endTime, string status)
        {

            StartTime = startTime;
            EndTime = endTime;
            Status = status;
        }
        public static DeliverySlot Create(DateTime startTime, DateTime endTime, string status = "booked")
        {
            if (startTime >= endTime)
                throw new ArgumentException("Start time must be before end time.");
            return new DeliverySlot(startTime, endTime, status);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return StartTime;
            yield return EndTime;
        }
    }
}
