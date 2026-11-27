using AutoMapper;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentDeliveryHistoryQueryHandler : QueryHandler<GetResidentDeliveryHistoryQuery, List<DeliveryHistoryDto>?>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Delivery> _deliveries;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageIntake> _intakes;
        private readonly IRepository<PackageReturn> _returns;

        public GetResidentDeliveryHistoryQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents,
            IRepository<Delivery> deliveries,
            IRepository<Package> packages,
            IRepository<PackageIntake> intakes,
            IRepository<PackageReturn> returns) : base(mapper)
        {
            _residents = residents;
            _deliveries = deliveries;
            _packages = packages;
            _intakes = intakes;
            _returns = returns;
        }

        protected override async Task<List<DeliveryHistoryDto>?> HandleAsync(GetResidentDeliveryHistoryQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident == null)
                return null;

            DateTime? fromUtc = request.FromDate is DateOnly from
                ? DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
                : null;
            // Inclusive ToDate: keep everything before the start of the next calendar day.
            DateTime? toExclusiveUtc = request.ToDate is DateOnly to
                ? DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
                : null;

            var deliveries = _deliveries
                .FetchMulti(d =>
                    d.ResidentId == resident.Id &&
                    (fromUtc == null || d.DeliveredAt >= fromUtc.Value) &&
                    (toExclusiveUtc == null || d.DeliveredAt < toExclusiveUtc.Value))
                .OrderByDescending(d => d.DeliveredAt)
                .ToList();
            var packageIds = deliveries.Select(d => d.PackageId).Distinct().ToList();
            var packages = packageIds.Count == 0
                ? new Dictionary<Guid, Package>()
                : _packages.GetAll()
                    .Where(p => packageIds.Contains(p.Id))
                    .AsEnumerable()
                    .ToDictionary(p => p.Id);

            // Bool-only — note/photo stay on package detail to keep history cheap.
            var damagedByPackage = packageIds.Count == 0
                ? new HashSet<Guid>()
                : (await _intakes.ListAsync(
                        intake => packageIds.Contains(intake.PackageId) && intake.IsDamaged,
                        cancellationToken: default,
                        noTracking: true).ConfigureAwait(false))
                    .Select(intake => intake.PackageId)
                    .ToHashSet();

            // Prefer an open return; otherwise the latest request for that package.
            var returnsByPackage = packageIds.Count == 0
                ? new Dictionary<Guid, PackageReturn>()
                : _returns.FetchMulti(
                        r => r.ResidentId == resident.Id && packageIds.Contains(r.PackageId),
                        noTracking: true)
                    .AsEnumerable()
                    .GroupBy(r => r.PackageId)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .OrderByDescending(r =>
                                r.Status is PackageReturnStatus.ReturnRequested
                                    or PackageReturnStatus.ReturnReceived
                                    or PackageReturnStatus.ReturnShelved)
                            .ThenByDescending(r => r.RequestedAtUtc)
                            .First());

            return deliveries
                .Select(d =>
                {
                    packages.TryGetValue(d.PackageId, out var package);
                    returnsByPackage.TryGetValue(d.PackageId, out var packageReturn);
                    return new DeliveryHistoryDto
                    {
                        Id = d.Id,
                        PackageId = d.PackageId,
                        DeliveredAt = d.DeliveredAt,
                        Notes = d.Notes,
                        Method = d.Method,
                        Status = d.Status.Value,
                        PackageStatus = package?.Status.ToString() ?? "Delivered",
                        PkgNumber = package?.PkgNumber ?? string.Empty,
                        TrackingNumber = package?.SphTrackingNumber?.Value,
                        CourierAwb = string.IsNullOrWhiteSpace(package?.CourierAwb)
                            ? null
                            : package!.CourierAwb.Trim(),
                        Courier = package?.CourierPartner,
                        Category = package?.Category,
                        IsDamaged = damagedByPackage.Contains(d.PackageId),
                        ReturnStatus = packageReturn?.Status.ToString(),
                        ReturnReason = packageReturn?.Reason.ToString(),
                        ReturnNote = packageReturn?.Note,
                        ReturnRequestedAtUtc = packageReturn?.RequestedAtUtc,
                    };
                })
                .ToList();
        }
    }
}
