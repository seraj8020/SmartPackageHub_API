using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Commands
{
    public sealed class RollupAnalyticsDailyCommandHandler
        : CreateCommandHandler<RollupAnalyticsDailyCommand, RollupAnalyticsDailyResult>
    {
        private static readonly TimeSpan OverdueThreshold = TimeSpan.FromHours(24);

        private readonly IRepository<Society> _societies;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Delivery> _deliveries;
        private readonly IRepository<AnalyticsDaily> _analytics;

        public RollupAnalyticsDailyCommandHandler(
            IRepository<Society> societies,
            IRepository<Package> packages,
            IRepository<Delivery> deliveries,
            IRepository<AnalyticsDaily> analytics,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _societies = societies;
            _packages = packages;
            _deliveries = deliveries;
            _analytics = analytics;
        }

        public override async Task<RollupAnalyticsDailyResult> Handle(
            RollupAnalyticsDailyCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var day = request.Day ?? AnalyticsScope.TodayUtc().AddDays(-1);
            var dayStart = AnalyticsScope.StartOfDayUtc(day);
            var dayEnd = AnalyticsScope.EndOfDayUtc(day);
            var overdueCutoff = dayEnd - OverdueThreshold;

            var societies = _societies.GetAll().AsEnumerable()
                .Where(s => AnalyticsScope.Includes(request.SocietyId, s.Id))
                .ToList();

            var packages = _packages.GetAll().AsEnumerable()
                .Where(p => AnalyticsScope.Includes(request.SocietyId, p.SocietyId))
                .ToList();

            var deliveries = _deliveries.GetAll().AsEnumerable()
                .Where(d => AnalyticsScope.Includes(request.SocietyId, d.SocietyId))
                .ToList();

            var existing = _analytics.GetAll(noTracking: false).AsEnumerable()
                .Where(a => a.Day == day && AnalyticsScope.Includes(request.SocietyId, a.SocietyId))
                .ToDictionary(a => a.SocietyId);

            var upserted = 0;
            foreach (var society in societies)
            {
                var societyPackages = packages.Where(p => p.SocietyId == society.Id).ToList();
                var received = societyPackages.Count(p => p.RecievedAt >= dayStart && p.RecievedAt <= dayEnd);
                var delivered = deliveries.Count(d =>
                    d.SocietyId == society.Id &&
                    d.DeliveredAt >= dayStart &&
                    d.DeliveredAt <= dayEnd);
                var overdue = societyPackages.Count(p =>
                    p.Status != PackageStatus.Delivered &&
                    p.RecievedAt <= overdueCutoff);

                if (existing.TryGetValue(society.Id, out var row))
                {
                    row.ReplaceCounts(received, delivered, overdue);
                    _analytics.Update(row);
                }
                else
                {
                    _analytics.Insert(AnalyticsDaily.Create(society.Id, day, received, delivered, overdue));
                }

                upserted++;
            }

            if (UnitOfWork is not null)
                await UnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new RollupAnalyticsDailyResult(
                day,
                societies.Count,
                upserted,
                $"Analytics rollup complete for {day:yyyy-MM-dd} ({upserted} societies).");
        }
    }
}
