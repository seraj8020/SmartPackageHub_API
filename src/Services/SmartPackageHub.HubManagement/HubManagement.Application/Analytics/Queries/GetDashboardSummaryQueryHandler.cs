using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetDashboardSummaryQueryHandler
        : QueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
    {
        private readonly IRepository<Society> _societies;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Delivery> _deliveries;

        public GetDashboardSummaryQueryHandler(
            IMapper mapper,
            IRepository<Society> societies,
            IRepository<Package> packages,
            IRepository<Delivery> deliveries) : base(mapper)
        {
            _societies = societies;
            _packages = packages;
            _deliveries = deliveries;
        }

        protected override Task<DashboardSummaryDto> HandleAsync(GetDashboardSummaryQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var today = AnalyticsScope.TodayUtc();
            var dayStart = AnalyticsScope.StartOfDayUtc(today);
            var dayEnd = AnalyticsScope.EndOfDayUtc(today);

            var societies = _societies.GetAll().AsEnumerable()
                .Where(s => AnalyticsScope.Includes(request.SocietyId, s.Id))
                .ToList();
            var packages = _packages.GetAll().AsEnumerable()
                .Where(p => AnalyticsScope.Includes(request.SocietyId, p.SocietyId))
                .ToList();
            var deliveries = _deliveries.GetAll().AsEnumerable()
                .Where(d => AnalyticsScope.Includes(request.SocietyId, d.SocietyId))
                .ToList();

            var dto = new DashboardSummaryDto(
                today,
                AnalyticsMetricsCalculator.CountReceived(packages, dayStart, dayEnd),
                AnalyticsMetricsCalculator.CountPending(packages),
                AnalyticsMetricsCalculator.CountDelivered(deliveries, dayStart, dayEnd),
                societies.Count,
                request.SocietyId is null || request.SocietyId == Guid.Empty
                    ? "All societies"
                    : societies.FirstOrDefault()?.Name ?? "Society");

            return Task.FromResult(dto);
        }
    }
}
