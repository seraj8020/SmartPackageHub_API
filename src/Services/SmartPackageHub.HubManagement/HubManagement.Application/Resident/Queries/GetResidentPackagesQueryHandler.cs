using AutoMapper;
using HubManagement.Application.DeliverySlots;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;
using HubManagement.Application.DeliverySlots.Helpers;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentPackagesQueryHandler : QueryHandler<GetResidentPackagesQuery, List<ResidentPackageDto>?>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<DeliverySlot> _slots;

        public GetResidentPackagesQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<DeliverySlot> slots) : base(mapper)
        {
            _residents = residents;
            _packages = packages;
            _slots = slots;
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

            return waiting.Select(p =>
            {
                activeSlots.TryGetValue(p.Id, out var slot);
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
                };
            }).ToList();
        }
    }
}
