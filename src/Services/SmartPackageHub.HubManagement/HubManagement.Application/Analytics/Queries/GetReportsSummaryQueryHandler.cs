using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetReportsSummaryQueryHandler
        : QueryHandler<GetReportsSummaryQuery, ReportsSummaryDto>
    {
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Delivery> _deliveries;

        public GetReportsSummaryQueryHandler(
            IMapper mapper,
            IRepository<Package> packages,
            IRepository<Delivery> deliveries) : base(mapper)
        {
            _packages = packages;
            _deliveries = deliveries;
        }

        protected override Task<ReportsSummaryDto> HandleAsync(GetReportsSummaryQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var days = NormalizeDays(request.Days);
            var toDay = AnalyticsScope.TodayUtc();
            var fromDay = toDay.AddDays(-(days - 1));
            var from = AnalyticsScope.StartOfDayUtc(fromDay);
            var to = AnalyticsScope.EndOfDayUtc(toDay);

            var packages = _packages.GetAll().AsEnumerable()
                .Where(p => AnalyticsScope.Includes(request.SocietyId, p.SocietyId))
                .ToList();
            var deliveries = _deliveries.GetAll().AsEnumerable()
                .Where(d => AnalyticsScope.Includes(request.SocietyId, d.SocietyId))
                .ToList();

            var received = AnalyticsMetricsCalculator.CountReceived(packages, from, to);
            var delivered = AnalyticsMetricsCalculator.CountDelivered(deliveries, from, to);
            var pending = AnalyticsMetricsCalculator.CountPending(packages);
            var avgHold = AnalyticsMetricsCalculator.AverageHoldDays(packages, deliveries, from, to);
            var rate = AnalyticsMetricsCalculator.MemberPickupRatePercent(received, delivered);

            return Task.FromResult(new ReportsSummaryDto(
                days,
                fromDay,
                toDay,
                received,
                avgHold,
                rate,
                received,
                delivered,
                pending));
        }

        internal static int NormalizeDays(int days) =>
            days switch
            {
                30 => 30,
                90 => 90,
                _ => 7
            };
    }
}
