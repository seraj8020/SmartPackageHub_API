using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;

namespace HubManagement.Application.Analytics.Services
{
    internal static class AnalyticsMetricsCalculator
    {
        public static int CountReceived(IEnumerable<Package> packages, DateTime fromInclusive, DateTime toInclusive) =>
            packages.Count(p => p.RecievedAt >= fromInclusive && p.RecievedAt <= toInclusive);

        public static int CountDelivered(IEnumerable<Delivery> deliveries, DateTime fromInclusive, DateTime toInclusive) =>
            deliveries.Count(d => d.DeliveredAt >= fromInclusive && d.DeliveredAt <= toInclusive);

        public static int CountPending(IEnumerable<Package> packages) =>
            packages.Count(p => p.Status != PackageStatus.Delivered);

        public static int CountPendingOpenAtEndOfDay(
            IEnumerable<Package> packages,
            IReadOnlyDictionary<Guid, DateTime> deliveredAtByPackage,
            DateTime dayEnd)
        {
            // Pending as of end of day: received on/before dayEnd and not delivered by dayEnd.
            return packages.Count(p =>
            {
                if (p.RecievedAt > dayEnd)
                    return false;
                if (!deliveredAtByPackage.TryGetValue(p.Id, out var deliveredAt))
                    return p.Status != PackageStatus.Delivered;
                return deliveredAt > dayEnd;
            });
        }

        public static double AverageHoldDays(
            IEnumerable<Package> packages,
            IEnumerable<Delivery> deliveries,
            DateTime fromInclusive,
            DateTime toInclusive)
        {
            var packageById = packages.ToDictionary(p => p.Id);
            var holds = new List<double>();
            foreach (var delivery in deliveries.Where(d => d.DeliveredAt >= fromInclusive && d.DeliveredAt <= toInclusive))
            {
                if (!packageById.TryGetValue(delivery.PackageId, out var package))
                    continue;
                var hours = (delivery.DeliveredAt - package.RecievedAt).TotalHours;
                if (hours >= 0)
                    holds.Add(hours / 24.0);
            }

            return holds.Count == 0 ? 0 : Math.Round(holds.Average(), 2);
        }

        public static double MemberPickupRatePercent(int received, int delivered) =>
            received <= 0 ? 0 : Math.Round(100.0 * delivered / received, 1);

        public static Dictionary<Guid, DateTime> FirstDeliveryByPackage(IEnumerable<Delivery> deliveries) =>
            deliveries
                .GroupBy(d => d.PackageId)
                .ToDictionary(g => g.Key, g => g.Min(d => d.DeliveredAt));
    }
}
