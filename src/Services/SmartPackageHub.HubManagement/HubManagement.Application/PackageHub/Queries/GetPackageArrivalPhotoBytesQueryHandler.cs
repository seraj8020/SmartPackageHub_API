using AutoMapper;
using HubManagement.Application.HubResidents.Queries;
using HubManagement.Application.HubResidents.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;

namespace HubManagement.Application.PackageHub.Queries
{
    public sealed class GetPackageArrivalPhotoBytesQueryHandler
        : QueryHandler<GetPackageArrivalPhotoBytesQuery, ResidentPhotoBytes?>
    {
        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageImage> _images;
        private readonly IParcelPhotoStore _photos;

        public GetPackageArrivalPhotoBytesQueryHandler(
            IMapper mapper,
            IRepository<Package> packages,
            IRepository<PackageImage> images,
            IParcelPhotoStore photos) : base(mapper)
        {
            _packages = packages;
            _images = images;
            _photos = photos;
        }

        protected override async Task<ResidentPhotoBytes?> HandleAsync(GetPackageArrivalPhotoBytesQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            if (package is null)
                return null;

            if (request.AllowedSocietyIds.Count > 0 &&
                !request.AllowedSocietyIds.Contains(package.SocietyId))
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
