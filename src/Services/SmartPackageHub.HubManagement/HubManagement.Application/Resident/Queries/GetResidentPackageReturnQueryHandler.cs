using AutoMapper;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Application.HubResidents.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class GetResidentPackageReturnQueryHandler
        : QueryHandler<GetResidentPackageReturnQuery, ResidentReturnRequestDto?>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageReturn> _returns;
        private readonly IRepository<PackageReturnImage> _returnImages;
        private readonly IParcelPhotoStore _photos;

        public GetResidentPackageReturnQueryHandler(
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

        protected override async Task<ResidentReturnRequestDto?> HandleAsync(GetResidentPackageReturnQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident is null)
                return null;

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            if (package is null || package.ResidentId != resident.Id)
                return null;

            var packageReturn = ResidentReturnRequestBuilder.FindForPackage(
                _returns,
                package.Id,
                resident.Id);
            if (packageReturn is null)
                return null;

            var returnPhotos = _returnImages
                .FetchMulti(i => i.ReturnId == packageReturn.Id, noTracking: true)
                .ToList();

            return await ResidentReturnRequestBuilder.ToDtoAsync(
                    packageReturn,
                    returnPhotos,
                    _photos,
                    resident.Id)
                .ConfigureAwait(false);
        }
    }
}
