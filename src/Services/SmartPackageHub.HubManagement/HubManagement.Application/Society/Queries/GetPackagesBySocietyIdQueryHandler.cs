using AutoMapper;
using HubManagement.Application.DeliverySlots;
using HubManagement.Application.DeliverySlots.Helpers;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Application.Societies.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Queries
{
    public sealed class GetPackagesBySocietyIdQueryHandler : QueryHandler<GetPackagesBySocietyIdQuery, List<SocietyPackageDto>?>
    {
        private readonly IRepository<Society> _societies;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<DeliverySlot> _slots;
        private readonly IRepository<PackageImage> _images;
        private readonly ILogger<GetPackagesBySocietyIdQueryHandler> _logger;

        public GetPackagesBySocietyIdQueryHandler(
            IMapper mapper,
            IRepository<Society> societies,
            IRepository<Package> packages,
            IRepository<Shelf> shelves,
            IRepository<DeliverySlot> slots,
            IRepository<PackageImage> images,
            ILogger<GetPackagesBySocietyIdQueryHandler> logger) : base(mapper)
        {
            _societies = societies;
            _packages = packages;
            _shelves = shelves;
            _slots = slots;
            _images = images;
            _logger = logger;
        }

        protected override async Task<List<SocietyPackageDto>?> HandleAsync(GetPackagesBySocietyIdQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var society = await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            if (society == null)
                return null;

            // Active hub packages for staff home / receive / shelf / route.
            // Include Dispatched so "Out for delivery" counts and caches stay correct
            // (Delivered stays on the dedicated deliveredpackages endpoint).
            var waiting = await _packages.ListAsync(
                p =>
                    p.SocietyId == society.Id &&
                    (p.Status == PackageStatus.Received ||
                     p.Status == PackageStatus.Shelved ||
                     p.Status == PackageStatus.Dispatched),
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
            var shelfCold = shelves.ToDictionary(shelf => shelf.Id, shelf => shelf.IsColdStorage);
            var activeSlots = SocietyPackageMapper.BuildActiveSlotLookup(ordered, slots);
            var arrivalPhotoIds = await SocietyArrivalPhotoFlags.ResolveSafeAsync(
                    _images, packageIds, _logger)
                .ConfigureAwait(false);

            return ordered.Select(p =>
            {
                activeSlots.TryGetValue(p.Id, out var slot);
                return SocietyPackageMapper.Map(
                    p,
                    shelfCodes,
                    slot: slot,
                    shelfCapacitiesById: shelfCapacities,
                    shelfColdById: shelfCold,
                    hasArrivalPhoto: arrivalPhotoIds.Contains(p.Id));
            }).ToList();
        }
    }
}
