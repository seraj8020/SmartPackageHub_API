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
        private readonly IRepository<AnalyticsDaily> _analytics;

        public GetReportsDailySeriesQueryHandler(
            IMapper mapper,
            IRepository<Package> packages,
            IRepository<Delivery> deliveries,
            IRepository<AnalyticsDaily> analytics) : base(mapper)
        {
            _packages = packages;
            _deliveries = deliveries;
            _analytics = analytics;
        }

        protected override Task<IReadOnlyList<ReportsDailyPointDto>> HandleAsync(GetReportsDailySeriesQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var days = GetReportsSummaryQueryHandler.NormalizeDays(request.Days);
            var toDay = AnalyticsScope.TodayUtc();
            var fromDay = toDay.AddDays(-(days - 1));

            var analytics = _analytics.GetAll().AsEnumerable()
                .Where(a =>
                    a.Day >= fromDay &&
                    a.Day <= toDay &&
                    AnalyticsScope.Includes(request.SocietyId, a.SocietyId))
                .ToList();

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
                var dayRows = analytics.Where(a => a.Day == day).ToList();

                int received;
                int delivered;
                if (dayRows.Count > 0)
                {
                    received = dayRows.Sum(a => a.PackagesReceived);
                    delivered = dayRows.Sum(a => a.PackagesDelivered);
                }
                else
                {
                    received = AnalyticsMetricsCalculator.CountReceived(packages, dayStart, dayEnd);
                    delivered = AnalyticsMetricsCalculator.CountDelivered(deliveries, dayStart, dayEnd);
                }

                var pending = AnalyticsMetricsCalculator.CountPendingOpenAtEndOfDay(packages, deliveredAt, dayEnd);
                points.Add(new ReportsDailyPointDto(day, received, delivered, pending));
            }

            return Task.FromResult<IReadOnlyList<ReportsDailyPointDto>>(points);
        }
    }
}
