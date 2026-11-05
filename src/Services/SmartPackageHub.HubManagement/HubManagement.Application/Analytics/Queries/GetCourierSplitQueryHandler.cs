using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetCourierSplitQueryHandler
        : QueryHandler<GetCourierSplitQuery, CourierSplitDashboardDto>
    {
        private readonly IRepository<AnalyticsDaily> _analytics;
        private readonly IRepository<Society> _societies;

        public GetCourierSplitQueryHandler(
            IMapper mapper,
            IRepository<AnalyticsDaily> analytics,
            IRepository<Society> societies) : base(mapper)
        {
            _analytics = analytics;
            _societies = societies;
        }

        protected override Task<CourierSplitDashboardDto> HandleAsync(GetCourierSplitQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var days = Math.Clamp(request.Days, 1, 90);
            var today = AnalyticsScope.TodayUtc();
            var from = today.AddDays(-(days - 1));
            var societyIds = _societies.GetAll()
                .Where(s => !request.SocietyId.HasValue || request.SocietyId == Guid.Empty || s.Id == request.SocietyId.Value)
                .Select(s => s.Id)
                .ToList();
            var analytics = societyIds.Count == 0
                ? new List<AnalyticsDaily>()
                : _analytics.GetAll()
                    .Where(a => a.Day >= from && a.Day <= today && societyIds.Contains(a.SocietyId))
                    .ToList();

            // Courier categories are persisted by each daily rollup in CourierSplitJson.
            var counts = analytics
                .Where(a => !string.IsNullOrWhiteSpace(a.CourierSplitJson))
                .SelectMany(a => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(a.CourierSplitJson!)
                    ?? new Dictionary<string, int>(StringComparer.Ordinal))
                .GroupBy(item => item.Key, StringComparer.Ordinal)
                .Select(g => new { Courier = g.Key, Count = g.Sum(item => item.Value) })
                .ToList();
            var total = counts.Sum(g => g.Count);
            var slices = counts
                .Select(g => new CourierSplitSliceDto(g.Courier, g.Count,
                    total == 0 ? 0 : Math.Round(100.0 * g.Count / total, 1)))
                .OrderByDescending(s => s.Count)
                .ToList();

            var lastRollupAtUtc = analytics
                .Where(a => a.LastRolledUpAtUtc.HasValue)
                .Max(a => a.LastRolledUpAtUtc);
            return Task.FromResult(new CourierSplitDashboardDto(slices, lastRollupAtUtc));
        }
    }
}
