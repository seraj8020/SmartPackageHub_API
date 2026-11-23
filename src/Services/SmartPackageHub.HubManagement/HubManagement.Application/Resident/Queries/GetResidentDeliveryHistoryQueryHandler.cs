using AutoMapper;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentDeliveryHistoryQueryHandler : QueryHandler<GetResidentDeliveryHistoryQuery, List<DeliveryHistoryDto>?>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Delivery> _deliveries;
        private readonly IRepository<Package> _packages;

        public GetResidentDeliveryHistoryQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents,
            IRepository<Delivery> deliveries,
            IRepository<Package> packages) : base(mapper)
        {
            _residents = residents;
            _deliveries = deliveries;
            _packages = packages;
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

            return deliveries
                .Select(d =>
                {
                    packages.TryGetValue(d.PackageId, out var package);
                    return new DeliveryHistoryDto
                    {
                        Id = d.Id,
                        PackageId = d.PackageId,
                        DeliveredAt = d.DeliveredAt,
                        Notes = d.Notes,
                        Method = d.Method,
                        Status = d.Status.Value,
                        PkgNumber = package?.PkgNumber ?? string.Empty,
                        Courier = package?.CourierPartner,
                        Category = package?.Category
                    };
                })
                .ToList();
        }
    }
}
