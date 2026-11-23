using AutoMapper;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentDamagePhotoBytesQueryHandler
        : QueryHandler<GetResidentDamagePhotoBytesQuery, ResidentDamagePhotoBytes?>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageIntake> _intakes;
        private readonly IParcelPhotoStore _photos;

        public GetResidentDamagePhotoBytesQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<PackageIntake> intakes,
            IParcelPhotoStore photos) : base(mapper)
        {
            _residents = residents;
            _packages = packages;
            _intakes = intakes;
            _photos = photos;
        }

        protected override async Task<ResidentDamagePhotoBytes?> HandleAsync(GetResidentDamagePhotoBytesQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident is null)
                return null;

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            if (package is null || package.ResidentId != resident.Id)
                return null;

            var intake = (await _intakes.ListAsync(
                    i => i.PackageId == package.Id && i.IsDamaged,
                    cancellationToken: default,
                    noTracking: true).ConfigureAwait(false))
                .OrderByDescending(i => i.ScannedAt)
                .FirstOrDefault();

            if (intake is null || string.IsNullOrWhiteSpace(intake.DamageImagePath))
                return null;

            var bytes = await _photos.ReadBytesAsync(intake.DamageImagePath).ConfigureAwait(false);
            if (bytes is null || bytes.Length == 0)
                return null;

            return new ResidentDamagePhotoBytes(bytes, "image/jpeg");
        }
    }
}
