using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetDashboardSummaryQueryHandler
        : QueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
    {
        private readonly IRepository<Society> _societies;
        private readonly IRepository<AnalyticsDaily> _analytics;
        private readonly IRepository<PackageReturn> _returns;
        private readonly IRepository<Package> _packages;

        public GetDashboardSummaryQueryHandler(
            IMapper mapper,
            IRepository<Society> societies,
            IRepository<AnalyticsDaily> analytics,
            IRepository<PackageReturn> returns,
            IRepository<Package> packages) : base(mapper)
        {
            _societies = societies;
            _analytics = analytics;
            _returns = returns;
            _packages = packages;
        }

        protected override Task<DashboardSummaryDto> HandleAsync(GetDashboardSummaryQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var todayUtc = AnalyticsScope.TodayUtc();
            var allSocieties = request.SocietyId is null || request.SocietyId == Guid.Empty;
            var scopedSocietyIds = _societies.GetAll()
                .Where(s => allSocieties || s.Id == request.SocietyId!.Value)
                .Select(s => s.Id)
                .ToList();
            var activeSocieties = scopedSocietyIds.Count;
            var societyName = "All societies";
            if (!allSocieties)
            {
                societyName = _societies.GetAll()
                    .Where(s => s.Id == request.SocietyId!.Value)
                    .Select(s => s.Name)
                    .FirstOrDefault() ?? "Society";
            }

            var societyIdSet = scopedSocietyIds.ToHashSet();
            var scopedAnalytics = _analytics.GetAll()
                .Where(a => societyIdSet.Contains(a.SocietyId))
                .ToList();

            // Today KPIs must use today's rollup only — never fall back to a prior day
            // (that made "Received today" show yesterday's count while the chart correctly
            // plotted 0 for today).
            var todayRows = scopedAnalytics.Where(a => a.Day == todayUtc).ToList();
            var packagesReceived = todayRows.Sum(a => a.PackagesReceived);
            var packagesDelivered = todayRows.Sum(a => a.PackagesDelivered);
            DateTime? lastRollupAtUtc = todayRows
                .Where(a => a.LastRolledUpAtUtc.HasValue)
                .Select(a => a.LastRolledUpAtUtc)
                .DefaultIfEmpty(null)
                .Max();

            // Pending is a live queue, not a daily received counter.
            var pendingPickup = _packages.GetAll()
                .AsEnumerable()
                .Count(p =>
                    societyIdSet.Contains(p.SocietyId) &&
                    p.Status != PackageStatus.Delivered);

            var last7From = todayUtc.AddDays(-6);
            var last7Rows = scopedAnalytics
                .Where(a => a.Day >= last7From && a.Day <= todayUtc)
                .ToList();
            var packagesLast7 = last7Rows.Sum(a => a.PackagesReceived);
            var deliveredLast7 = last7Rows.Sum(a => a.PackagesDelivered);

            var returns = _returns.GetAll().AsEnumerable()
                .Where(r => societyIdSet.Contains(r.SocietyId))
                .ToList();
            var todayStart = AnalyticsScope.StartOfDayUtc(todayUtc);
            var tomorrowStart = AnalyticsScope.StartOfDayUtc(todayUtc.AddDays(1));
            var last7Start = AnalyticsScope.StartOfDayUtc(last7From);
            var returnedToday = AnalyticsMetricsCalculator.CountReturned(returns, todayStart, tomorrowStart);
            var returnedLast7 = AnalyticsMetricsCalculator.CountReturned(returns, last7Start, tomorrowStart);

            var societiesRolledUp = todayRows
                .Select(a => a.SocietyId)
                .Distinct()
                .Count();
            var dto = new DashboardSummaryDto(
                todayUtc,
                packagesReceived,
                pendingPickup,
                packagesDelivered,
                activeSocieties,
                societyName,
                lastRollupAtUtc,
                societiesRolledUp,
                packagesLast7,
                deliveredLast7,
                returnedToday,
                returnedLast7);

            return Task.FromResult(dto);
        }
    }
}
