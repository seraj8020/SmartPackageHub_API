using AutoMapper;
using HubManagement.Application.DeliverySlots;
using HubManagement.Application.DeliverySlots.Helpers;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Application.HubResidents.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentPackageDetailQueryHandler
        : QueryHandler<GetResidentPackageDetailQuery, ResidentPackageDetailDto?>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<DeliverySlot> _slots;
        private readonly IRepository<PackageIntake> _intakes;
        private readonly IParcelPhotoStore _photos;

        public GetResidentPackageDetailQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<DeliverySlot> slots,
            IRepository<PackageIntake> intakes,
            IParcelPhotoStore photos) : base(mapper)
        {
            _residents = residents;
            _packages = packages;
            _slots = slots;
            _intakes = intakes;
            _photos = photos;
        }

        protected override async Task<ResidentPackageDetailDto?> HandleAsync(GetResidentPackageDetailQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident is null)
                return null;

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            if (package is null || package.ResidentId != resident.Id)
                return null;

            var slotRows = await _slots.ListAsync(
                    slot =>
                        slot.PackageId == package.Id &&
                        slot.ResidentId == resident.Id &&
                        slot.Status != DeliverySlotWindows.StatusCancelled,
                    cancellationToken: default,
                    noTracking: true).ConfigureAwait(false);
            var slot = slotRows.OrderByDescending(s => s.WindowStart).FirstOrDefault();

            var intake = (await _intakes.ListAsync(
                    i => i.PackageId == package.Id,
                    cancellationToken: default,
                    noTracking: true).ConfigureAwait(false))
                .OrderByDescending(i => i.ScannedAt)
                .FirstOrDefault();

            HubIntakeDamageNoteDto? hubNote = null;
            var isDamaged = intake?.IsDamaged == true;
            if (isDamaged && intake is not null)
            {
                var (url, expires) = await ResidentDamagePhotoUrls.ResolveAsync(
                        _photos,
                        resident.Id,
                        package.Id,
                        intake.DamageImagePath).ConfigureAwait(false);

                hubNote = new HubIntakeDamageNoteDto
                {
                    DamageNote = intake.DamageNote,
                    DamagePhotoUrl = url,
                    DamagePhotoExpiresAtUtc = expires,
                };
            }

            return new ResidentPackageDetailDto
            {
                Id = package.Id,
                TrackingNumber = package.SphTrackingNumber.Value,
                ReceivedAt = package.RecievedAt,
                Courier = package.CourierPartner,
                Category = package.Category,
                Weight = package.Dimensions.Weight,
                FlatNumber = package.FlatNumber.Value,
                PkgNumber = package.PkgNumber,
                Status = package.Status.ToString(),
                IsCod = package.IsCod,
                CodAmount = package.CodAmount,
                CodApprovalStatus = package.CodApprovalStatus.ToString(),
                DeliverySlotStatus = slot?.Status,
                DeliverySlotWindowStart = slot?.WindowStart,
                DeliverySlotWindowEnd = slot?.WindowEnd,
                DeliverySlotLabel = slot == null
                    ? null
                    : DeliverySlotWindows.LabelFor(slot.WindowStart, slot.WindowEnd),
                PreferredDeliveryMethod = package.PreferredDeliveryMethod,
                IsDamaged = isDamaged,
                HubIntakeNote = hubNote,
            };
        }
    }
}
