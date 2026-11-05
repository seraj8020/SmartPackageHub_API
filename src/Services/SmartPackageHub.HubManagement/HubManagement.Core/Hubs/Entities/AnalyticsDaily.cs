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
        public int PendingCountVersion { get; private set; }
        public string CourierSplitJson { get; private set; }
        public DateTime? LastRolledUpAtUtc { get; private set; }

        private AnalyticsDaily(Guid societyId, DateOnly day, int packagesReceived, int packagesDelivered, int overdueCount, DateTime? lastRolledUpAtUtc, string courierSplitJson = "{}", int pendingCountVersion = 0)
        {
            SocietyId = societyId;
            Day = day;
            PackagesReceived = packagesReceived;
            PackagesDelivered = packagesDelivered;
            OverdueCount = overdueCount;
            PendingCountVersion = pendingCountVersion;
            CourierSplitJson = courierSplitJson;
            LastRolledUpAtUtc = lastRolledUpAtUtc;
        }
        public static AnalyticsDaily Create(Guid societyId, DateOnly day, int packagesReceived, int packagesDelivered, int overdueCount, DateTime? lastRolledUpAtUtc = null, string courierSplitJson = "{}", int pendingCountVersion = 0)
        {
            return new AnalyticsDaily(societyId, day, packagesReceived, packagesDelivered, overdueCount, lastRolledUpAtUtc, courierSplitJson, pendingCountVersion);
        }

        /// <summary>Replaces daily counts (used by end-of-day rollup upsert).</summary>
        public void ReplaceCounts(int packagesReceived, int packagesDelivered, int overdueCount, DateTime lastRolledUpAtUtc, string courierSplitJson = "{}", int pendingCountVersion = 1)
        {
            if (packagesReceived < 0 || packagesDelivered < 0 || overdueCount < 0)
                throw new ArgumentOutOfRangeException(nameof(packagesReceived), "Counts cannot be negative.");

            PackagesReceived = packagesReceived;
            PackagesDelivered = packagesDelivered;
            OverdueCount = overdueCount;
            LastRolledUpAtUtc = DateTime.SpecifyKind(lastRolledUpAtUtc, DateTimeKind.Utc);
            CourierSplitJson = courierSplitJson;
            PendingCountVersion = pendingCountVersion;
        }
    }
}
