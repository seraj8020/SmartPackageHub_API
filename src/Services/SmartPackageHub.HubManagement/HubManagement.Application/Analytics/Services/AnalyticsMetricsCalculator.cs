using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;

namespace HubManagement.Application.Analytics.Services
{
    internal static class AnalyticsMetricsCalculator
    {
        public static int CountReceived(IEnumerable<Package> packages, DateTime fromInclusive, DateTime toExclusive) =>
            packages.Count(p => p.RecievedAt >= fromInclusive && p.RecievedAt < toExclusive);

        public static int CountDelivered(IEnumerable<Delivery> deliveries, DateTime fromInclusive, DateTime toExclusive) =>
            deliveries.Where(d => d.Status.Equals(DeliveryStatus.Completed) && d.DeliveredAt >= fromInclusive && d.DeliveredAt < toExclusive)
                .Select(d => d.PackageId).Distinct().Count();

        public static int CountPending(IEnumerable<Package> packages) =>
            packages.Count(p => p.Status != PackageStatus.Delivered);

        public static int CountPendingOpenBefore(
            IEnumerable<Package> packages,
            IReadOnlyDictionary<Guid, DateTime> deliveredAtByPackage,
            DateTime dayEndExclusive)
        {
            return packages.Count(p =>
            {
                if (p.RecievedAt >= dayEndExclusive)
                    return false;
                if (!deliveredAtByPackage.TryGetValue(p.Id, out var deliveredAt))
                    return true;
                return deliveredAt >= dayEndExclusive;
            });
        }

        public static int CountPendingOpenAtEndOfDay(
            IEnumerable<Package> packages,
            IReadOnlyDictionary<Guid, DateTime> deliveredAtByPackage,
            DateTime dayEndExclusive,
            DateTime? receivedFromInclusive)
        {
            var receivedFrom = receivedFromInclusive ?? DateTime.MinValue;
            return packages.Count(p =>
            {
                if (p.RecievedAt < receivedFrom || p.RecievedAt >= dayEndExclusive)
                    return false;
                return !deliveredAtByPackage.TryGetValue(p.Id, out var deliveredAt) || deliveredAt >= dayEndExclusive;
            });
        }

        public static int CountPendingOpenAtEndOfDay(
            IEnumerable<Package> packages,
            IReadOnlyDictionary<Guid, DateTime> deliveredAtByPackage,
            DateTime dayEndExclusive) =>
            CountPendingOpenAtEndOfDay(packages, deliveredAtByPackage, dayEndExclusive, null);

        public static double AverageHoldDays(
            IEnumerable<Package> packages,
            IEnumerable<Delivery> deliveries,
            DateTime fromInclusive,
            DateTime toExclusive)
        {
            var holds = CollectHoldHours(packages, deliveries, fromInclusive, toExclusive);
            return holds.Count == 0 ? 0 : Math.Round(holds.Average() / 24.0, 2);
        }

        public static (double MinHours, double MaxHours, double MedianHours, double AverageHours) HoldHoursStats(
            IEnumerable<Package> packages,
            IEnumerable<Delivery> deliveries,
            DateTime fromInclusive,
            DateTime toExclusive)
        {
            var holds = CollectHoldHours(packages, deliveries, fromInclusive, toExclusive);
            if (holds.Count == 0)
                return (0, 0, 0, 0);

            holds.Sort();
            var min = holds[0];
            var max = holds[^1];
            var avg = holds.Average();
            var mid = holds.Count / 2;
            var median = holds.Count % 2 == 0
                ? (holds[mid - 1] + holds[mid]) / 2.0
                : holds[mid];

            return (
                Math.Round(min, 1),
                Math.Round(max, 1),
                Math.Round(median, 1),
                Math.Round(avg, 1));
        }

        public static int CountReturned(
            IEnumerable<PackageReturn> returns,
            DateTime fromInclusive,
            DateTime toExclusive) =>
            returns.Count(r =>
            {
                var at = r.ReceivedAtUtc ?? r.RequestedAtUtc;
                return at >= fromInclusive && at < toExclusive;
            });

        private static List<double> CollectHoldHours(
            IEnumerable<Package> packages,
            IEnumerable<Delivery> deliveries,
            DateTime fromInclusive,
            DateTime toExclusive)
        {
            var packageById = packages.ToDictionary(p => p.Id);
            var holds = new List<double>();
            foreach (var delivery in deliveries.Where(d =>
                         d.Status.Equals(DeliveryStatus.Completed) &&
                         d.DeliveredAt >= fromInclusive &&
                         d.DeliveredAt < toExclusive))
            {
                if (!packageById.TryGetValue(delivery.PackageId, out var package))
                    continue;
                var hours = (delivery.DeliveredAt - package.RecievedAt).TotalHours;
                if (hours >= 0)
                    holds.Add(hours);
            }

            return holds;
        }

        /// <summary>
        /// Legacy ratio of deliveries-in-range / receipts-in-range. Capped at 100% because
        /// deliveries of packages received before the window can otherwise inflate the rate.
        /// Prefer <see cref="MemberPickupRateOfReceivedPercent"/> for reports.
        /// </summary>
        public static double MemberPickupRatePercent(int received, int delivered) =>
            received <= 0 ? 0 : Math.Round(Math.Min(100.0, 100.0 * delivered / received), 1);

        /// <summary>
        /// Of packages received in the window, percent that were delivered by the window end.
        /// Never exceeds 100%.
        /// </summary>
        public static double MemberPickupRateOfReceivedPercent(
            IEnumerable<Package> packages,
            IReadOnlyDictionary<Guid, DateTime> deliveredAtByPackage,
            DateTime fromInclusive,
            DateTime toInclusive)
        {
            var received = packages
                .Where(p => p.RecievedAt >= fromInclusive && p.RecievedAt <= toInclusive)
                .ToList();
            if (received.Count == 0)
                return 0;

            var pickedUp = received.Count(p =>
                deliveredAtByPackage.TryGetValue(p.Id, out var deliveredAt) &&
                deliveredAt <= toInclusive);

            return Math.Round(100.0 * pickedUp / received.Count, 1);
        }

        public static Dictionary<Guid, DateTime> FirstDeliveryByPackage(IEnumerable<Delivery> deliveries) =>
            deliveries
                .Where(d => d.Status.Equals(DeliveryStatus.Completed))
                .GroupBy(d => d.PackageId)
                .ToDictionary(g => g.Key, g => g.Min(d => d.DeliveredAt));
    }
}
