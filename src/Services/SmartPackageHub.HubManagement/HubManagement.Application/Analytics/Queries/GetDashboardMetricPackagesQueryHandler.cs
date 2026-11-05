using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetDashboardMetricPackagesQueryHandler
        : QueryHandler<GetDashboardMetricPackagesQuery, DashboardMetricPackagesPageDto>
    {
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Delivery> _deliveries;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<AnalyticsDaily> _analytics;

        public GetDashboardMetricPackagesQueryHandler(
            IMapper mapper,
            IRepository<Package> packages,
            IRepository<Delivery> deliveries,
            IRepository<Society> societies,
            IRepository<AnalyticsDaily> analytics) : base(mapper)
        {
            _packages = packages;
            _deliveries = deliveries;
            _societies = societies;
            _analytics = analytics;
        }

        protected override Task<DashboardMetricPackagesPageDto> HandleAsync(GetDashboardMetricPackagesQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var day = request.Day ?? AnalyticsScope.TodayUtc().AddDays(-1);
            var (deliveredMetric, _, receivedMetric) = GetMetricFlags(request.Metric);
            var (page, pageSize, skip) = GetPageValues(request);
            var start = AnalyticsScope.StartOfDayUtc(day);
            var end = AnalyticsScope.StartOfDayUtc(day.AddDays(1));

            var names = _societies.GetAll()
                .Where(s => request.SocietyId == null || request.SocietyId == Guid.Empty || s.Id == request.SocietyId.Value)
                .ToDictionary(s => s.Id, s => s.Name);
            var dayRollups = _analytics.GetAll()
                .Where(a => a.Day == day &&
                    (request.SocietyId == null || request.SocietyId == Guid.Empty || a.SocietyId == request.SocietyId.Value));
            IQueryable<Delivery> completedDeliveries = _deliveries.GetAll()
                .Where(d => d.Status.Value == "Completed" &&
                    dayRollups.Any(a => a.SocietyId == d.SocietyId &&
                        (a.LastRolledUpAtUtc == null || a.LastRolledUpAtUtc >= end
                            ? d.DeliveredAt < end
                            : d.DeliveredAt <= a.LastRolledUpAtUtc.Value)));
            var scopedPackages = _packages.GetAll()
                .Where(p => dayRollups.Any(a => a.SocietyId == p.SocietyId &&
                    (a.LastRolledUpAtUtc == null || a.LastRolledUpAtUtc >= end
                        ? p.RecievedAt < end
                        : p.RecievedAt <= a.LastRolledUpAtUtc.Value)));
            var receivedPackages = scopedPackages.Where(p => p.RecievedAt >= start && p.RecievedAt < end);
            var rows = new List<DashboardMetricPackageDto>();
            int totalCount;
            if (deliveredMetric)
            {
                var matchingPackages = scopedPackages.Where(p =>
                    completedDeliveries.Any(d => d.PackageId == p.Id && d.SocietyId == p.SocietyId &&
                        d.DeliveredAt >= start && d.DeliveredAt < end));
                totalCount = matchingPackages.Count();
                var packagePage = matchingPackages.OrderByDescending(p => completedDeliveries
                        .Where(d => d.PackageId == p.Id && d.SocietyId == p.SocietyId &&
                            d.DeliveredAt >= start && d.DeliveredAt < end)
                        .Min(d => d.DeliveredAt))
                    .ThenBy(p => p.Id).Skip(skip).Take(pageSize).ToList();
                var packageIds = packagePage.Select(p => p.Id).ToList();
                var deliveredAtByPackage = completedDeliveries.Where(d => packageIds.Contains(d.PackageId) &&
                        d.DeliveredAt >= start && d.DeliveredAt < end)
                    .AsEnumerable()
                    .GroupBy(d => d.PackageId)
                    .ToDictionary(group => group.Key, group => group.Min(d => d.DeliveredAt));
                foreach (var package in packagePage)
                {
                    names.TryGetValue(package.SocietyId, out var societyName);
                    rows.Add(new DashboardMetricPackageDto(
                        package.Id,
                        package.PkgNumber,
                        package.RecipientName,
                        package.RecievedAt,
                        deliveredAtByPackage.GetValueOrDefault(package.Id),
                        package.CourierPartner,
                        package.TowerWing,
                        package.FlatNumber.Value,
                        package.Status.ToString(),
                        package.SocietyId,
                        societyName));
                }
            }
            else if (receivedMetric)
            {
                var matching = receivedPackages.Where(p => p.RecievedAt >= start && p.RecievedAt < end);
                totalCount = matching.Count();
                foreach (var package in matching.OrderByDescending(p => p.RecievedAt).ThenBy(p => p.Id)
                             .Skip(skip).Take(pageSize).ToList())
                {
                    names.TryGetValue(package.SocietyId, out var societyName);
                    rows.Add(new DashboardMetricPackageDto(
                        package.Id,
                        package.PkgNumber,
                        package.RecipientName,
                        package.RecievedAt,
                        null,
                        package.CourierPartner,
                        package.TowerWing,
                        package.FlatNumber.Value,
                        package.Status.ToString(),
                        package.SocietyId,
                        societyName));
                }
            }
            else
            {
                var deliveredPackageIds = completedDeliveries.Where(d => d.DeliveredAt < end)
                    .Select(d => d.PackageId).Distinct();
                var matching = _packages.GetAll().Where(p => dayRollups.Any(a => a.SocietyId == p.SocietyId &&
                        (p.RecievedAt < end &&
                         (a.PendingCountVersion != 0 || p.RecievedAt >= start)) &&
                        p.RecievedAt <= (a.LastRolledUpAtUtc ?? end)) &&
                    !deliveredPackageIds.Contains(p.Id));
                totalCount = matching.Count();
                foreach (var package in matching.OrderByDescending(p => p.RecievedAt).ThenBy(p => p.Id)
                             .Skip(skip).Take(pageSize).ToList())
                {
                    names.TryGetValue(package.SocietyId, out var societyName);
                    rows.Add(new DashboardMetricPackageDto(
                        package.Id,
                        package.PkgNumber,
                        package.RecipientName,
                        package.RecievedAt,
                        null,
                        package.CourierPartner,
                        package.TowerWing,
                        package.FlatNumber.Value,
                        package.Status.ToString(),
                        package.SocietyId,
                        societyName));
                }
            }

            return Task.FromResult(new DashboardMetricPackagesPageDto(rows, totalCount, page, pageSize));
        }

        private static (bool Delivered, bool Pending, bool Received) GetMetricFlags(string metric)
        {
            var delivered = string.Equals(metric, "delivered", StringComparison.OrdinalIgnoreCase);
            var pending = string.Equals(metric, "pending", StringComparison.OrdinalIgnoreCase);
            var received = string.Equals(metric, "received", StringComparison.OrdinalIgnoreCase);
            if (!delivered && !pending && !received)
                throw new ArgumentException("Metric must be received, delivered, or pending.", nameof(metric));
            return (delivered, pending, received);
        }

        private static (int Page, int PageSize, int Skip) GetPageValues(GetDashboardMetricPackagesQuery request)
        {
            if (request.Day == DateOnly.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(request), "Day must be earlier than the maximum supported date.");
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
            return (page, pageSize, skip);
        }
    }
}
