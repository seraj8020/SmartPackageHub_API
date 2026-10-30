using AutoMapper;
using HubManagement.Application.DeliverySlots;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;
using HubManagement.Application.DeliverySlots.Helpers;

namespace HubManagement.Application.Societies.Queries
{
    public sealed class GetPackagesBySocietyIdQueryHandler : QueryHandler<GetPackagesBySocietyIdQuery, List<SocietyPackageDto>?>
    {
        private readonly IRepository<Society> _societies;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<DeliverySlot> _slots;

        public GetPackagesBySocietyIdQueryHandler(
            IMapper mapper,
            IRepository<Society> societies,
            IRepository<Package> packages,
            IRepository<Shelf> shelves,
            IRepository<DeliverySlot> slots) : base(mapper)
        {
            _societies = societies;
            _packages = packages;
            _shelves = shelves;
            _slots = slots;
        }

        protected override async Task<List<SocietyPackageDto>?> HandleAsync(GetPackagesBySocietyIdQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var society = await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            if (society == null)
                return null;

            // All hub packages still waiting � not limited to today, so older shelved
            // (not yet dispatched) and received items remain visible on staff screens.
            var waiting = await _packages.ListAsync(
                p =>
                    p.SocietyId == society.Id &&
                    (p.Status == PackageStatus.Received || p.Status == PackageStatus.Shelved),
                noTracking: true).ConfigureAwait(false);

            if (waiting.Count == 0)
                return [];

            var ordered = waiting
                .OrderByDescending(package => package.RecievedAt)
                .ToList();

            var shelfIds = ordered
                .Where(package => package.ShelfId.HasValue)
                .Select(package => package.ShelfId!.Value)
                .Distinct()
                .ToList();

            var shelves = shelfIds.Count == 0
                ? []
                : await _shelves.ListAsync(shelf => shelfIds.Contains(shelf.Id), noTracking: true).ConfigureAwait(false);

            var packageIds = ordered.Select(p => p.Id).ToList();
            var slots = await _slots.ListAsync(
                slot =>
                    slot.SocietyId == society.Id &&
                    slot.Status != DeliverySlotWindows.StatusCancelled &&
                    packageIds.Contains(slot.PackageId),
                noTracking: true).ConfigureAwait(false);

            var shelfCodes = SocietyPackageMapper.BuildShelfCodeLookup(ordered, shelves);
            var shelfCapacities = shelves.ToDictionary(shelf => shelf.Id, shelf => shelf.Capacity);
            var activeSlots = SocietyPackageMapper.BuildActiveSlotLookup(ordered, slots);

            return ordered.Select(p =>
            {
                activeSlots.TryGetValue(p.Id, out var slot);
                return SocietyPackageMapper.Map(p, shelfCodes, slot: slot, shelfCapacitiesById: shelfCapacities);
            }).ToList();
        }
    }
}
