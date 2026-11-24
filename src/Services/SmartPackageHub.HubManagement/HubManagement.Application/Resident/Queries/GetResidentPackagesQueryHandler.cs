using AutoMapper;
using HubManagement.Application.DeliverySlots;
using HubManagement.Application.DeliverySlots.Helpers;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Application.HubResidents.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentPackagesQueryHandler : QueryHandler<GetResidentPackagesQuery, List<ResidentPackageDto>?>
    {
        private static readonly Action<ILogger, int, Exception?> LogArrivalPhotoFlagsFailed =
            LoggerMessage.Define<int>(
                LogLevel.Warning,
                new EventId(1, nameof(GetResidentPackagesQueryHandler)),
                "Could not resolve arrival-photo flags for {PackageCount} inbox packages; returning packages without HasArrivalPhoto.");

        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<DeliverySlot> _slots;
        private readonly IRepository<PackageIntake> _intakes;
        private readonly IRepository<PackageImage> _images;
        private readonly IRepository<Shelf> _shelves;
        private readonly ILogger<GetResidentPackagesQueryHandler> _logger;

        public GetResidentPackagesQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<DeliverySlot> slots,
            IRepository<PackageIntake> intakes,
            IRepository<PackageImage> images,
            IRepository<Shelf> shelves,
            ILogger<GetResidentPackagesQueryHandler> logger) : base(mapper)
        {
            _residents = residents;
            _packages = packages;
            _slots = slots;
            _intakes = intakes;
            _images = images;
            _shelves = shelves;
            _logger = logger;
        }

        protected override async Task<List<ResidentPackageDto>?> HandleAsync(GetResidentPackagesQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident == null)
                return null;

            // Inbox = shelved (+ dispatched). Also include pending COD so the resident can approve/reject.
            var waiting = await _packages.ListAsync(
                p =>
                    p.ResidentId == resident.Id &&
                    (
                        p.Status == PackageStatus.Shelved ||
                        p.Status == PackageStatus.Dispatched ||
                        (p.IsCod &&
                         p.CodApprovalStatus == CodApprovalStatus.Pending &&
                         p.Status == PackageStatus.Received)
                    ),
                cancellationToken: default,
                noTracking: true).ConfigureAwait(false);

            if (waiting.Count == 0)
                return [];

            var packageIds = waiting.Select(p => p.Id).ToHashSet();
            var slotRows = await _slots.ListAsync(
                slot =>
                    slot.ResidentId == resident.Id &&
                    slot.Status != DeliverySlotWindows.StatusCancelled,
                cancellationToken: default,
                noTracking: true).ConfigureAwait(false);

            var activeSlots = slotRows
                .Where(slot => packageIds.Contains(slot.PackageId))
                .GroupBy(slot => slot.PackageId)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(slot => slot.WindowStart).First());

            // Bool-only damage flag — never load note/photo bytes on the inbox list.
            var damagedByPackage = (await _intakes.ListAsync(
                    intake => packageIds.Contains(intake.PackageId) && intake.IsDamaged,
                    cancellationToken: default,
                    noTracking: true).ConfigureAwait(false))
                .Select(intake => intake.PackageId)
                .ToHashSet();

            // Arrival-photo flag must never take down the inbox (photo store / EF edge cases).
            var arrivalPhotoPackageIds = await ResolveArrivalPhotoPackageIdsSafeAsync(packageIds)
                .ConfigureAwait(false);

            var shelfIds = waiting
                .Where(p => p.ShelfId.HasValue)
                .Select(p => p.ShelfId!.Value)
                .Distinct()
                .ToHashSet();
            var shelvesById = shelfIds.Count == 0
                ? new Dictionary<Guid, Shelf>()
                : (await _shelves.ListAsync(
                        shelf => shelfIds.Contains(shelf.Id),
                        cancellationToken: default,
                        noTracking: true).ConfigureAwait(false))
                    .ToDictionary(shelf => shelf.Id);

            return waiting
                .OrderByDescending(p => p.RecievedAt)
                .Select(p =>
                {
                    activeSlots.TryGetValue(p.Id, out var slot);
                    Shelf? shelf = null;
                    if (p.ShelfId.HasValue)
                        shelvesById.TryGetValue(p.ShelfId.Value, out shelf);

                    return new ResidentPackageDto
                    {
                        Id = p.Id,
                        TrackingNumber = p.SphTrackingNumber.Value,
                        ReceivedAt = p.RecievedAt,
                        Courier = p.CourierPartner,
                        Category = p.Category,
                        Weight = p.Dimensions.Weight,
                        FlatNumber = p.FlatNumber.Value,
                        PkgNumber = p.PkgNumber,
                        Status = p.Status.ToString(),
                        IsCod = p.IsCod,
                        CodAmount = p.CodAmount,
                        CodApprovalStatus = p.CodApprovalStatus.ToString(),
                        DeliverySlotStatus = slot?.Status,
                        DeliverySlotWindowStart = slot?.WindowStart,
                        DeliverySlotWindowEnd = slot?.WindowEnd,
                        DeliverySlotLabel = slot == null
                            ? null
                            : DeliverySlotWindows.LabelFor(slot.WindowStart, slot.WindowEnd),
                        PreferredDeliveryMethod = p.PreferredDeliveryMethod,
                        IsDamaged = damagedByPackage.Contains(p.Id),
                        HasArrivalPhoto = arrivalPhotoPackageIds.Contains(p.Id),
                        ShelfCode = shelf?.Code,
                        ShelfTowerOrWing = shelf?.TowerOrWing,
                        ShelfZone = shelf?.Location?.Zone,
                        ShelfAisle = shelf?.Location?.Aisle,
                        ShelfTier = shelf?.Location is null ? null : shelf.Location.Tier,
                        IsColdStorage = shelf?.IsColdStorage == true || p.IsColdStorage,
                    };
                }).ToList();
        }

        private async Task<HashSet<Guid>> ResolveArrivalPhotoPackageIdsSafeAsync(HashSet<Guid> packageIds)
        {
            if (packageIds.Count == 0)
                return [];

            try
            {
                // EF-safe predicate only; Kind / empty StorageKey filtered in memory.
                var rows = await _images.ListAsync(
                        image => packageIds.Contains(image.PackageId),
                        cancellationToken: default,
                        noTracking: true)
                    .ConfigureAwait(false);
                return ResidentArrivalPhotoLookup.PackageIdsWithArrivalPhoto(rows, packageIds);
            }
#pragma warning disable CA1031 // Inbox must still return packages if photo flag lookup fails.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogArrivalPhotoFlagsFailed(_logger, packageIds.Count, ex);
                return [];
            }
        }
    }
}
