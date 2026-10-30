using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetCourierSplitQueryHandler
        : QueryHandler<GetCourierSplitQuery, IReadOnlyList<CourierSplitSliceDto>>
    {
        private readonly IRepository<Package> _packages;

        public GetCourierSplitQueryHandler(IMapper mapper, IRepository<Package> packages) : base(mapper)
        {
            _packages = packages;
        }

        protected override Task<IReadOnlyList<CourierSplitSliceDto>> HandleAsync(GetCourierSplitQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var days = Math.Clamp(request.Days, 1, 90);
            var today = AnalyticsScope.TodayUtc();
            var from = AnalyticsScope.StartOfDayUtc(today.AddDays(-(days - 1)));
            var to = AnalyticsScope.EndOfDayUtc(today);

            var packages = _packages.GetAll().AsEnumerable()
                .Where(p =>
                    AnalyticsScope.Includes(request.SocietyId, p.SocietyId) &&
                    p.RecievedAt >= from &&
                    p.RecievedAt <= to)
                .ToList();

            var total = packages.Count;
            if (total == 0)
                return Task.FromResult<IReadOnlyList<CourierSplitSliceDto>>(Array.Empty<CourierSplitSliceDto>());

            var slices = packages
                .GroupBy(p => string.IsNullOrWhiteSpace(p.CourierPartner) ? "Unknown" : p.CourierPartner.Trim())
                .Select(g => new CourierSplitSliceDto(
                    g.Key,
                    g.Count(),
                    Math.Round(100.0 * g.Count() / total, 1)))
                .OrderByDescending(s => s.Count)
                .ToList();

            return Task.FromResult<IReadOnlyList<CourierSplitSliceDto>>(slices);
        }
    }
}
