using AutoMapper;
using HubManagement.Application.HubResidents.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentArrivalPhotoBytesQueryHandler
        : QueryHandler<GetResidentArrivalPhotoBytesQuery, ResidentPhotoBytes?>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageImage> _images;
        private readonly IParcelPhotoStore _photos;

        public GetResidentArrivalPhotoBytesQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<PackageImage> images,
            IParcelPhotoStore photos) : base(mapper)
        {
            _residents = residents;
            _packages = packages;
            _images = images;
            _photos = photos;
        }

        protected override async Task<ResidentPhotoBytes?> HandleAsync(GetResidentArrivalPhotoBytesQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident is null)
                return null;

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            if (package is null || package.ResidentId != resident.Id)
                return null;

            var image = ResidentArrivalPhotoLookup.FindForPackage(_images, package.Id);
            if (image is null || string.IsNullOrWhiteSpace(image.StorageKey))
                return null;

            var bytes = await _photos.ReadBytesAsync(image.StorageKey).ConfigureAwait(false);
            if (bytes is null || bytes.Length == 0)
                return null;

            var contentType = string.IsNullOrWhiteSpace(image.ContentType) ? "image/jpeg" : image.ContentType;
            return new ResidentPhotoBytes(bytes, contentType);
        }
    }
}
