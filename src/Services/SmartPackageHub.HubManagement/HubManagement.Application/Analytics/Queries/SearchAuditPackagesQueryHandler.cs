using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class SearchAuditPackagesQueryHandler
        : QueryHandler<SearchAuditPackagesQuery, AuditPackageSearchPageDto>
    {
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<Delivery> _deliveries;

        public SearchAuditPackagesQueryHandler(
            IMapper mapper,
            IRepository<Package> packages,
            IRepository<Society> societies,
            IRepository<Delivery> deliveries) : base(mapper)
        {
            _packages = packages;
            _societies = societies;
            _deliveries = deliveries;
        }

        protected override Task<AuditPackageSearchPageDto> HandleAsync(SearchAuditPackagesQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var societyFilter = request.SocietyId is Guid sid && sid != Guid.Empty;
            var q = string.IsNullOrWhiteSpace(request.Q) ? null : request.Q.Trim();
            var courier = string.IsNullOrWhiteSpace(request.Courier) ? null : request.Courier.Trim();

            DateTime? fromUtc = request.FromDate is DateOnly from
                ? DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
                : null;
            DateTime? toExclusiveUtc = request.ToDate is DateOnly to
                ? DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
                : null;

            var query = _packages.GetAll()
                .Where(p => !societyFilter || p.SocietyId == request.SocietyId!.Value)
                .Where(p => fromUtc == null || p.RecievedAt >= fromUtc.Value)
                .Where(p => toExclusiveUtc == null || p.RecievedAt < toExclusiveUtc.Value)
                .AsEnumerable();

            if (courier is not null)
            {
                query = query.Where(p =>
                    !string.IsNullOrWhiteSpace(p.CourierPartner) &&
                    p.CourierPartner.Contains(courier, StringComparison.OrdinalIgnoreCase));
            }

            if (q is not null)
            {
                query = query.Where(p =>
                    (!string.IsNullOrWhiteSpace(p.RecipientName) &&
                     p.RecipientName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(p.CourierPartner) &&
                     p.CourierPartner.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(p.PkgNumber) &&
                     p.PkgNumber.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(p.CourierAwb) &&
                     p.CourierAwb.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (p.SphTrackingNumber != null &&
                     p.SphTrackingNumber.Value.Contains(q, StringComparison.OrdinalIgnoreCase)));
            }

            var matched = query
                .OrderByDescending(p => p.RecievedAt)
                .ThenByDescending(p => p.Id)
                .ToList();

            var total = matched.Count;
            var pageRows = matched.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var societyNames = _societies.GetAll()
                .Where(s => pageRows.Select(p => p.SocietyId).Distinct().Contains(s.Id))
                .ToDictionary(s => s.Id, s => s.Name);

            var packageIds = pageRows.Select(p => p.Id).ToList();
            var deliveredAt = packageIds.Count == 0
                ? new Dictionary<Guid, DateTime>()
                : _deliveries.GetAll()
                    .Where(d => packageIds.Contains(d.PackageId) && d.Status.Value == "Completed")
                    .AsEnumerable()
                    .GroupBy(d => d.PackageId)
                    .ToDictionary(g => g.Key, g => g.Max(d => d.DeliveredAt));

            var items = pageRows.Select(p =>
            {
                societyNames.TryGetValue(p.SocietyId, out var societyName);
                deliveredAt.TryGetValue(p.Id, out var delivered);
                return new AuditPackageSearchItemDto
                {
                    Id = p.Id,
                    PackageNumber = p.PkgNumber,
                    TrackingNumber = p.SphTrackingNumber?.Value,
                    CourierAwb = p.CourierAwb,
                    RecipientName = p.RecipientName,
                    Courier = p.CourierPartner,
                    Status = p.Status.ToString(),
                    ReceivedAtUtc = p.RecievedAt,
                    DeliveredAtUtc = p.Status == PackageStatus.Delivered ? delivered : null,
                    TowerWing = p.TowerWing,
                    FlatNumber = p.FlatNumber.Value,
                    SocietyId = p.SocietyId,
                    SocietyName = societyName,
                };
            }).ToList();

            return Task.FromResult(new AuditPackageSearchPageDto
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
            });
        }
    }
}
