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

            var scopedAnalytics = _analytics.GetAll()
                .Where(a => scopedSocietyIds.Contains(a.SocietyId))
                .ToList();

            // Prefer today's UTC rollup when any scoped society has it; otherwise latest available day.
            // Societies without a row for the snapshot day contribute 0 to KPI sums.
            var hasTodayRollup = scopedAnalytics.Any(a => a.Day == todayUtc);
            var latestRollupDay = scopedAnalytics
                .Select(a => (DateOnly?)a.Day)
                .OrderByDescending(day => day)
                .FirstOrDefault();
            var snapshotDay = hasTodayRollup ? todayUtc : (latestRollupDay ?? todayUtc);
            var snapshotBySociety = scopedAnalytics
                .Where(a => a.Day == snapshotDay)
                .GroupBy(a => a.SocietyId)
                .ToDictionary(group => group.Key, group => group.First());

            var packagesReceived = 0;
            var pendingPickup = 0;
            var packagesDelivered = 0;
            DateTime? lastRollupAtUtc = null;
            foreach (var societyId in scopedSocietyIds)
            {
                if (!snapshotBySociety.TryGetValue(societyId, out var row))
                    continue;

                packagesReceived += row.PackagesReceived;
                pendingPickup += row.OverdueCount;
                packagesDelivered += row.PackagesDelivered;
                if (row.LastRolledUpAtUtc.HasValue &&
                    (lastRollupAtUtc is null || row.LastRolledUpAtUtc > lastRollupAtUtc))
                {
                    lastRollupAtUtc = row.LastRolledUpAtUtc;
                }
            }

            var societiesRolledUp = snapshotBySociety.Count;
            var dto = new DashboardSummaryDto(
                snapshotDay,
                packagesReceived,
                pendingPickup,
                packagesDelivered,
                activeSocieties,
                societyName,
                lastRollupAtUtc,
                societiesRolledUp);

            return Task.FromResult(dto);
        }
    }
}
