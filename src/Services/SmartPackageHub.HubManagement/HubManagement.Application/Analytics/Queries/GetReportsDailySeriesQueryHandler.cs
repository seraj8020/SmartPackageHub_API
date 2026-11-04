using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetReportsDailySeriesQueryHandler
        : QueryHandler<GetReportsDailySeriesQuery, IReadOnlyList<ReportsDailyPointDto>>
    {
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Delivery> _deliveries;

        public GetReportsDailySeriesQueryHandler(
            IMapper mapper,
            IRepository<Package> packages,
            IRepository<Delivery> deliveries) : base(mapper)
        {
            _packages = packages;
            _deliveries = deliveries;
        }

        protected override Task<IReadOnlyList<ReportsDailyPointDto>> HandleAsync(GetReportsDailySeriesQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var days = GetReportsSummaryQueryHandler.NormalizeDays(request.Days);
            var toDay = AnalyticsScope.TodayUtc();
            var fromDay = toDay.AddDays(-(days - 1));

            // Always use live package/delivery counts. Partial AnalyticsDaily rollups across
            // societies previously undercounted platform-wide days when only some societies had rows.
            var packages = _packages.GetAll().AsEnumerable()
                .Where(p => AnalyticsScope.Includes(request.SocietyId, p.SocietyId))
                .ToList();
            var deliveries = _deliveries.GetAll().AsEnumerable()
                .Where(d => AnalyticsScope.Includes(request.SocietyId, d.SocietyId))
                .ToList();
            var deliveredAt = AnalyticsMetricsCalculator.FirstDeliveryByPackage(deliveries);

            var points = new List<ReportsDailyPointDto>(days);
            for (var i = 0; i < days; i++)
            {
                var day = fromDay.AddDays(i);
                var dayStart = AnalyticsScope.StartOfDayUtc(day);
                var dayEnd = AnalyticsScope.EndOfDayUtc(day);
                var received = AnalyticsMetricsCalculator.CountReceived(packages, dayStart, dayEnd);
                var delivered = AnalyticsMetricsCalculator.CountDelivered(deliveries, dayStart, dayEnd);
                var pending = AnalyticsMetricsCalculator.CountPendingOpenAtEndOfDay(packages, deliveredAt, dayEnd);
                points.Add(new ReportsDailyPointDto(day, received, delivered, pending));
            }

            return Task.FromResult<IReadOnlyList<ReportsDailyPointDto>>(points);
        }
    }
}
