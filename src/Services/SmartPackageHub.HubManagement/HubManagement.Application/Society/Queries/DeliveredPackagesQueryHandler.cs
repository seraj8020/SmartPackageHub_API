using AutoMapper;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Queries
{
    public sealed class DeliveredPackagesQueryHandler : QueryHandler<DeliveredPackagesQuery, List<SocietyPackageDto>?>
    {
        private readonly IRepository<Society> _societies;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<Delivery> _deliveries;

        public DeliveredPackagesQueryHandler(
            IMapper mapper,
            IRepository<Society> societies,
            IRepository<Package> packages,
            IRepository<Shelf> shelves,
            IRepository<Delivery> deliveries) : base(mapper)
        {
            _societies = societies;
            _packages = packages;
            _shelves = shelves;
            _deliveries = deliveries;
        }

        protected override async Task<List<SocietyPackageDto>?> HandleAsync(DeliveredPackagesQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var society = await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            if (society == null)
                return null;

            var waiting = await _packages.ListAsync(
                p => p.SocietyId == society.Id && p.Status == PackageStatus.Delivered,
                noTracking: true).ConfigureAwait(false);

            if (waiting.Count == 0)
                return [];

            var packageIds = waiting.Select(p => p.Id).ToHashSet();
            var deliveries = await _deliveries.ListAsync(
                d => d.SocietyId == society.Id && packageIds.Contains(d.PackageId),
                noTracking: true).ConfigureAwait(false);

            var deliveredAtByPackage = deliveries
                .GroupBy(d => d.PackageId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Max(d => d.DeliveredAt));

            var shelfIds = waiting
                .Where(package => package.ShelfId.HasValue)
                .Select(package => package.ShelfId!.Value)
                .Distinct()
                .ToList();

            var shelves = shelfIds.Count == 0
                ? []
                : await _shelves.ListAsync(shelf => shelfIds.Contains(shelf.Id), noTracking: true).ConfigureAwait(false);

            var shelfCodes = SocietyPackageMapper.BuildShelfCodeLookup(waiting, shelves);
            return waiting
                .OrderByDescending(p =>
                    deliveredAtByPackage.TryGetValue(p.Id, out var at) ? at : p.RecievedAt)
                .Select(p =>
                {
                    deliveredAtByPackage.TryGetValue(p.Id, out var deliveredAt);
                    return SocietyPackageMapper.Map(p, shelfCodes, deliveredAt == default ? null : deliveredAt);
                })
                .ToList();
        }
    }
}
