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

            var day = request.Day ?? AnalyticsScope.TodayUtc();
            var rolledUpAtUtc = DateTime.UtcNow;
            var dayStart = AnalyticsScope.StartOfDayUtc(day);
            var dayEndExclusive = AnalyticsScope.StartOfDayUtc(day.AddDays(1));

            var allSocieties = request.SocietyId is null || request.SocietyId == Guid.Empty;
            var societies = _societies.GetAll()
                .Where(s => allSocieties || s.Id == request.SocietyId!.Value)
                .ToList();
            var societyIds = societies.Select(society => society.Id).ToList();

            var receivedBySociety = _packages.GetAll()
                .Where(p => societyIds.Contains(p.SocietyId) && p.RecievedAt >= dayStart &&
                    p.RecievedAt < dayEndExclusive && p.RecievedAt <= rolledUpAtUtc)
                .GroupBy(p => p.SocietyId)
                .Select(group => new { SocietyId = group.Key, Count = group.Count() })
                .ToDictionary(row => row.SocietyId, row => row.Count);

            var deliveredBySociety = _deliveries.GetAll()
                .Where(d => societyIds.Contains(d.SocietyId) && d.Status.Value == "Completed" &&
                    d.DeliveredAt >= dayStart && d.DeliveredAt < dayEndExclusive && d.DeliveredAt <= rolledUpAtUtc)
                .GroupBy(d => d.SocietyId)
                .Select(group => new
                {
                    SocietyId = group.Key,
                    Count = group.Select(d => d.PackageId).Distinct().Count()
                })
                .ToDictionary(row => row.SocietyId, row => row.Count);

            var courierCountsBySociety = _packages.GetAll()
                .Where(p => societyIds.Contains(p.SocietyId) && p.RecievedAt >= dayStart &&
                    p.RecievedAt < dayEndExclusive && p.RecievedAt <= rolledUpAtUtc)
                .GroupBy(p => new { p.SocietyId, p.CourierPartner })
                .Select(group => new { group.Key.SocietyId, group.Key.CourierPartner, Count = group.Count() })
                .AsEnumerable()
                .GroupBy(row => row.SocietyId)
                .ToDictionary(
                    group => group.Key,
                    group => System.Text.Json.JsonSerializer.Serialize(group
                        .GroupBy(row => string.IsNullOrWhiteSpace(row.CourierPartner) ? "Unknown" : row.CourierPartner.Trim(), StringComparer.Ordinal)
                        .ToDictionary(courier => courier.Key, courier => courier.Sum(row => row.Count), StringComparer.Ordinal)));

            var completedPackagesBeforeEnd = _deliveries.GetAll()
                .Where(d => societyIds.Contains(d.SocietyId) && d.Status.Value == "Completed" &&
                    d.DeliveredAt < dayEndExclusive && d.DeliveredAt <= rolledUpAtUtc)
                .Select(d => d.PackageId);
            var pendingBySociety = _packages.GetAll()
                .Where(p => societyIds.Contains(p.SocietyId) &&
                    p.RecievedAt < dayEndExclusive && p.RecievedAt <= rolledUpAtUtc &&
                    !completedPackagesBeforeEnd.Contains(p.Id))
                .GroupBy(p => p.SocietyId)
                .Select(group => new { SocietyId = group.Key, Count = group.Count() })
                .ToDictionary(row => row.SocietyId, row => row.Count);

            var existing = _analytics.GetAll(noTracking: false)
                .Where(a => a.Day == day && (allSocieties || a.SocietyId == request.SocietyId!.Value))
                .ToDictionary(a => a.SocietyId);

            var upserted = 0;
            foreach (var societyId in societies.Select(society => society.Id))
            {
                var received = receivedBySociety.GetValueOrDefault(societyId);
                var delivered = deliveredBySociety.GetValueOrDefault(societyId);
                var pending = pendingBySociety.GetValueOrDefault(societyId);
                var courierSplitJson = courierCountsBySociety.GetValueOrDefault(societyId, "{}");

                if (existing.TryGetValue(societyId, out var row))
                {
                    row.ReplaceCounts(received, delivered, pending, rolledUpAtUtc, courierSplitJson, pendingCountVersion: 1);
                    _analytics.Update(row);
                }
                else
                {
                    _analytics.Insert(AnalyticsDaily.Create(societyId, day, received, delivered, pending, rolledUpAtUtc, courierSplitJson, pendingCountVersion: 1));
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
