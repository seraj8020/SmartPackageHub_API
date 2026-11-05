using System.Globalization;
using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetWeeklyDeliveryTrendQueryHandler
        : QueryHandler<GetWeeklyDeliveryTrendQuery, IReadOnlyList<WeeklyTrendPointDto>>
    {
        private readonly IRepository<AnalyticsDaily> _analytics;
        private readonly IRepository<Society> _societies;

        public GetWeeklyDeliveryTrendQueryHandler(
            IMapper mapper,
            IRepository<AnalyticsDaily> analytics,
            IRepository<Society> societies) : base(mapper)
        {
            _analytics = analytics;
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
            var points = new List<WeeklyTrendPointDto>(days);
            for (var i = 0; i < days; i++)
            {
                var day = fromDay.AddDays(i);
                var dayRows = analytics.Where(a => a.Day == day).ToList();
                var received = dayRows.Sum(a => a.PackagesReceived);
                var delivered = dayRows.Sum(a => a.PackagesDelivered);
                var pending = dayRows.Sum(a => a.OverdueCount);
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
