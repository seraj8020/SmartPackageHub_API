using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetTopScannersQueryHandler
        : QueryHandler<GetTopScannersQuery, TopScannersDashboardDto>
    {
        private readonly IRepository<Package> _packages;
        private readonly IRepository<User> _users;

        public GetTopScannersQueryHandler(
            IMapper mapper,
            IRepository<Package> packages,
            IRepository<User> users) : base(mapper)
        {
            _packages = packages;
            _users = users;
        }

        protected override Task<TopScannersDashboardDto> HandleAsync(GetTopScannersQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var days = Math.Clamp(request.Days, 1, 90);
            var take = Math.Clamp(request.Take, 1, 20);
            var toDay = AnalyticsScope.TodayUtc();
            var fromDay = toDay.AddDays(-(days - 1));
            var from = AnalyticsScope.StartOfDayUtc(fromDay);
            var toExclusive = AnalyticsScope.StartOfDayUtc(toDay.AddDays(1));

            var counts = _packages.GetAll().AsEnumerable()
                .Where(p => AnalyticsScope.Includes(request.SocietyId, p.SocietyId))
                .Where(p => p.RecievedAt >= from && p.RecievedAt < toExclusive)
                .GroupBy(p => p.StaffUserId)
                .Select(g => new { StaffUserId = g.Key, ScanCount = g.Count() })
                .OrderByDescending(x => x.ScanCount)
                .Take(take)
                .ToList();

            var userIds = counts.Select(c => c.StaffUserId).ToHashSet();
            var names = _users.GetAll().AsEnumerable()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.Username);

            var items = counts
                .Select(c => new TopScannerDto(
                    c.StaffUserId,
                    names.TryGetValue(c.StaffUserId, out var name) && !string.IsNullOrWhiteSpace(name)
                        ? name
                        : "Unknown staff",
                    c.ScanCount))
                .ToList();

            return Task.FromResult(new TopScannersDashboardDto(items, days, fromDay, toDay));
        }
    }
}
