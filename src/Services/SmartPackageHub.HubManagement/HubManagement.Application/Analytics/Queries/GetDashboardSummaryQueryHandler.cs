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
        private readonly IRepository<AnalyticsDaily> _analytics;

        public GetDashboardSummaryQueryHandler(
            IMapper mapper,
            IRepository<Society> societies,
            IRepository<AnalyticsDaily> analytics) : base(mapper)
        {
            _societies = societies;
            _analytics = analytics;
        }

        protected override Task<DashboardSummaryDto> HandleAsync(GetDashboardSummaryQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var summaryDay = AnalyticsScope.TodayUtc();
            var allSocieties = request.SocietyId is null || request.SocietyId == Guid.Empty;
            var scopedSocieties = _societies.GetAll()
                .Where(s => allSocieties || s.Id == request.SocietyId!.Value);
            var activeSocieties = scopedSocieties.Count();
            var societyName = allSocieties
                ? "All societies"
                : scopedSocieties.Select(s => s.Name).FirstOrDefault() ?? "Society";

            var analytics = _analytics.GetAll()
                .Where(a => a.Day == summaryDay &&
                    (allSocieties || a.SocietyId == request.SocietyId!.Value));

            var dto = new DashboardSummaryDto(
                summaryDay,
                analytics.Sum(a => (int?)a.PackagesReceived) ?? 0,
                analytics.Sum(a => (int?)a.OverdueCount) ?? 0,
                analytics.Sum(a => (int?)a.PackagesDelivered) ?? 0,
                activeSocieties,
                societyName,
                analytics.Max(a => a.LastRolledUpAtUtc));

            return Task.FromResult(dto);
        }

    }
}
