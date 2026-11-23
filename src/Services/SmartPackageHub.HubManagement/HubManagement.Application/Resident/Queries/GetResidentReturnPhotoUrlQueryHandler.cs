using AutoMapper;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Application.HubResidents.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentReturnPhotoUrlQueryHandler
        : QueryHandler<GetResidentReturnPhotoUrlQuery, ReturnPhotoUrlDto?>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageReturn> _returns;
        private readonly IRepository<PackageReturnImage> _returnImages;
        private readonly IParcelPhotoStore _photos;

        public GetResidentReturnPhotoUrlQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<PackageReturn> returns,
            IRepository<PackageReturnImage> returnImages,
            IParcelPhotoStore photos) : base(mapper)
        {
            _residents = residents;
            _packages = packages;
            _returns = returns;
            _returnImages = returnImages;
            _photos = photos;
        }

        protected override async Task<ReturnPhotoUrlDto?> HandleAsync(GetResidentReturnPhotoUrlQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident is null)
                return null;

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            if (package is null || package.ResidentId != resident.Id)
                return null;

            var photo = await _returnImages.GetByIdAsync(request.PhotoId).ConfigureAwait(false);
            if (photo is null)
                return null;

            var packageReturn = await _returns.GetByIdAsync(photo.ReturnId).ConfigureAwait(false);
            if (packageReturn is null
                || packageReturn.PackageId != package.Id
                || packageReturn.ResidentId != resident.Id)
            {
                return null;
            }

            var (url, expires) = await ResidentReturnPhotoUrls.ResolveAsync(
                    _photos,
                    resident.Id,
                    package.Id,
                    photo.Id,
                    photo.StorageKey)
                .ConfigureAwait(false);

            return new ReturnPhotoUrlDto
            {
                PhotoId = photo.Id,
                PhotoUrl = url,
                PhotoExpiresAtUtc = expires,
            };
        }
    }
}
