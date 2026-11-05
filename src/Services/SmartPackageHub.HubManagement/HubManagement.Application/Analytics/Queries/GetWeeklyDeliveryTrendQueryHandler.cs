using System.Globalization;
using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetWeeklyDeliveryTrendQueryHandler
        : QueryHandler<GetWeeklyDeliveryTrendQuery, IReadOnlyList<WeeklyTrendPointDto>>
    {
        private readonly IRepository<AnalyticsDaily> _analytics;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Delivery> _deliveries;
        private readonly IRepository<Society> _societies;

        public GetWeeklyDeliveryTrendQueryHandler(
            IMapper mapper,
            IRepository<AnalyticsDaily> analytics,
            IRepository<Package> packages,
            IRepository<Delivery> deliveries,
            IRepository<Society> societies) : base(mapper)
        {
            _analytics = analytics;
            _packages = packages;
            _deliveries = deliveries;
            _societies = societies;
        }

        protected override Task<IReadOnlyList<WeeklyTrendPointDto>> HandleAsync(GetWeeklyDeliveryTrendQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var days = Math.Clamp(request.Days, 1, 90);
            var throughDay = AnalyticsScope.TodayUtc();
            var fromDay = throughDay.AddDays(-(days - 1));
            var societyIds = _societies.GetAll()
                .Where(s => !request.SocietyId.HasValue || request.SocietyId == Guid.Empty || s.Id == request.SocietyId.Value)
                .Select(s => s.Id)
                .ToList();
            if (societyIds.Count == 0)
                return Task.FromResult<IReadOnlyList<WeeklyTrendPointDto>>(Array.Empty<WeeklyTrendPointDto>());

            var analytics = _analytics.GetAll()
                .Where(a => a.Day >= fromDay && a.Day <= throughDay && societyIds.Contains(a.SocietyId))
                .ToList();
            var rolledDaysBySociety = analytics
                .Select(a => (a.SocietyId, a.Day))
                .ToHashSet();
            var fromStart = AnalyticsScope.StartOfDayUtc(fromDay);
            var throughEnd = AnalyticsScope.StartOfDayUtc(throughDay.AddDays(1));
            var missingSocietyIds = societyIds
                .Where(id => Enumerable.Range(0, days).Any(i => !rolledDaysBySociety.Contains((id, fromDay.AddDays(i)))))
                .ToList();
            var packages = missingSocietyIds.Count == 0
                ? new List<Package>()
                : _packages.GetAll()
                    .Where(p => missingSocietyIds.Contains(p.SocietyId) && p.RecievedAt >= fromStart && p.RecievedAt < throughEnd)
                    .ToList();
            var deliveries = missingSocietyIds.Count == 0
                ? new List<Delivery>()
                : _deliveries.GetAll()
                    .Where(d => missingSocietyIds.Contains(d.SocietyId) && d.DeliveredAt >= fromStart && d.DeliveredAt < throughEnd)
                    .ToList();
            var packagesBySociety = packages.GroupBy(p => p.SocietyId).ToDictionary(g => g.Key, g => g.ToList());
            var deliveriesBySociety = deliveries.GroupBy(d => d.SocietyId).ToDictionary(g => g.Key, g => g.ToList());
            var points = new List<WeeklyTrendPointDto>(days);
            for (var i = 0; i < days; i++)
            {
                var day = fromDay.AddDays(i);
                var dayRows = analytics.Where(a => a.Day == day).ToList();
                var dayStart = AnalyticsScope.StartOfDayUtc(day);
                var dayEnd = AnalyticsScope.StartOfDayUtc(day.AddDays(1));
                var rolledSocietyIds = dayRows.Select(a => a.SocietyId).ToHashSet();
                var missingForDay = societyIds.Where(id => !rolledSocietyIds.Contains(id)).ToList();
                var received = dayRows.Sum(a => a.PackagesReceived);
                var delivered = dayRows.Sum(a => a.PackagesDelivered);
                var pending = dayRows.Sum(a => a.OverdueCount);
                if (missingForDay.Count > 0)
                {
                    var deliveredPackageIds = _deliveries.GetAll()
                        .Where(d => missingForDay.Contains(d.SocietyId) && d.Status.Value == "Completed" && d.DeliveredAt < dayEnd)
                        .Select(d => d.PackageId)
                        .Distinct();
                    var pendingBySociety = _packages.GetAll()
                        .Where(p => missingForDay.Contains(p.SocietyId) && p.RecievedAt < dayEnd &&
                            !deliveredPackageIds.Contains(p.Id))
                        .GroupBy(p => p.SocietyId)
                        .Select(group => new { SocietyId = group.Key, Count = group.Count() })
                        .ToDictionary(row => row.SocietyId, row => row.Count);
                    pending += pendingBySociety.Values.Sum();
                }
                foreach (var societyId in missingForDay)
                {
                    packagesBySociety.TryGetValue(societyId, out var societyPackages);
                    deliveriesBySociety.TryGetValue(societyId, out var societyDeliveries);
                    societyPackages ??= new List<Package>();
                    societyDeliveries ??= new List<Delivery>();
                    received += AnalyticsMetricsCalculator.CountReceived(societyPackages, dayStart, dayEnd);
                    delivered += AnalyticsMetricsCalculator.CountDelivered(societyDeliveries, dayStart, dayEnd);
                }
                points.Add(new WeeklyTrendPointDto(
                    day,
                    day.ToString("ddd", CultureInfo.InvariantCulture),
                    received,
                    delivered,
                    pending));
            }

            return Task.FromResult<IReadOnlyList<WeeklyTrendPointDto>>(points);
        }

    }
}
