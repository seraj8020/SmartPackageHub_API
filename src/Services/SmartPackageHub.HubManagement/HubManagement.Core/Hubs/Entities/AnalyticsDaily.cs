using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class AnalyticsDaily : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; private set; }
        public DateOnly Day { get; private set; }
        public int PackagesReceived { get; private set; }
        public int PackagesDelivered { get; private set; }
        public int OverdueCount { get; private set; }

        private AnalyticsDaily(Guid societyId, DateOnly day, int packagesReceived, int packagesDelivered, int overdueCount)
        {
            SocietyId = societyId;
            Day = day;
            PackagesReceived = packagesReceived;
            PackagesDelivered = packagesDelivered;
            OverdueCount = overdueCount;
        }
        public static AnalyticsDaily Create(Guid societyId, DateOnly day, int packagesReceived, int packagesDelivered, int overdueCount)
        {
            return new AnalyticsDaily(societyId, day, packagesReceived, packagesDelivered, overdueCount);
        }
    }
}
