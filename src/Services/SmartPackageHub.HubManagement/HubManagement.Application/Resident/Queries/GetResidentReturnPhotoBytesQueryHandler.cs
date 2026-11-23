using AutoMapper;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentReturnPhotoBytesQueryHandler
        : QueryHandler<GetResidentReturnPhotoBytesQuery, ResidentPhotoBytes?>
    {

        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageReturn> _returns;
        private readonly IRepository<PackageReturnImage> _returnImages;
        private readonly IParcelPhotoStore _photos;

        public GetResidentReturnPhotoBytesQueryHandler(
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

        protected override async Task<ResidentPhotoBytes?> HandleAsync(GetResidentReturnPhotoBytesQuery request)
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

            var bytes = await _photos.ReadBytesAsync(photo.StorageKey).ConfigureAwait(false);
            if (bytes is null || bytes.Length == 0)
                return null;

            var contentType = string.IsNullOrWhiteSpace(photo.ContentType) ? "image/jpeg" : photo.ContentType;
            return new ResidentPhotoBytes(bytes, contentType);
        }
    }
}
