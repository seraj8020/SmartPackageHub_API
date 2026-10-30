using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetSocietyPerformanceQueryHandler
        : QueryHandler<GetSocietyPerformanceQuery, IReadOnlyList<SocietyPerformanceDto>>
    {
        private readonly IRepository<Society> _societies;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Delivery> _deliveries;

        public GetSocietyPerformanceQueryHandler(
            IMapper mapper,
            IRepository<Society> societies,
            IRepository<Package> packages,
            IRepository<Delivery> deliveries) : base(mapper)
        {
            _societies = societies;
            _packages = packages;
            _deliveries = deliveries;
        }

        protected override Task<IReadOnlyList<SocietyPerformanceDto>> HandleAsync(GetSocietyPerformanceQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var days = GetReportsSummaryQueryHandler.NormalizeDays(request.Days);
            var toDay = AnalyticsScope.TodayUtc();
            var fromDay = toDay.AddDays(-(days - 1));
            var from = AnalyticsScope.StartOfDayUtc(fromDay);
            var to = AnalyticsScope.EndOfDayUtc(toDay);

            var societies = _societies.GetAll().AsEnumerable()
                .Where(s => AnalyticsScope.Includes(request.SocietyId, s.Id))
                .OrderBy(s => s.Name)
                .ToList();
            var packages = _packages.GetAll().AsEnumerable()
                .Where(p => AnalyticsScope.Includes(request.SocietyId, p.SocietyId))
                .ToList();
            var deliveries = _deliveries.GetAll().AsEnumerable()
                .Where(d => AnalyticsScope.Includes(request.SocietyId, d.SocietyId))
                .ToList();

            var rows = societies.Select(society =>
            {
                var societyPackages = packages.Where(p => p.SocietyId == society.Id).ToList();
                var societyDeliveries = deliveries.Where(d => d.SocietyId == society.Id).ToList();
                var received = AnalyticsMetricsCalculator.CountReceived(societyPackages, from, to);
                var delivered = AnalyticsMetricsCalculator.CountDelivered(societyDeliveries, from, to);
                var pending = AnalyticsMetricsCalculator.CountPending(societyPackages);
                var avgHold = AnalyticsMetricsCalculator.AverageHoldDays(societyPackages, societyDeliveries, from, to);
                var rate = AnalyticsMetricsCalculator.MemberPickupRatePercent(received, delivered);
                return new SocietyPerformanceDto(
                    society.Id,
                    society.Name,
                    received,
                    delivered,
                    pending,
                    avgHold,
                    rate);
            }).ToList();

            return Task.FromResult<IReadOnlyList<SocietyPerformanceDto>>(rows);
        }
    }
}
