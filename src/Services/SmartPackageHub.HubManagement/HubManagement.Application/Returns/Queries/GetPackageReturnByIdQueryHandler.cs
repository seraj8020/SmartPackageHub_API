using AutoMapper;
using HubManagement.Application.Returns.DTOs;
using HubManagement.Application.Returns.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Returns.Queries
{
    public sealed class GetPackageReturnByIdQueryHandler
        : QueryHandler<GetPackageReturnByIdQuery, PackageReturnDto?>
    {
        private readonly IRepository<PackageReturn> _returns;
        private readonly IRepository<PackageReturnImage> _images;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Shelf> _shelves;

        public GetPackageReturnByIdQueryHandler(
            IMapper mapper,
            IRepository<PackageReturn> returns,
            IRepository<PackageReturnImage> images,
            IRepository<Package> packages,
            IRepository<Resident> residents,
            IRepository<Shelf> shelves) : base(mapper)
        {
            _returns = returns;
            _images = images;
            _packages = packages;
            _residents = residents;
            _shelves = shelves;
        }

        protected override async Task<PackageReturnDto?> HandleAsync(GetPackageReturnByIdQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var packageReturn = await _returns.GetByIdAsync(request.ReturnId).ConfigureAwait(false);
            if (packageReturn is null)
                return null;

            var package = await _packages.GetByIdAsync(packageReturn.PackageId).ConfigureAwait(false);
            var resident = await _residents.GetByIdAsync(packageReturn.ResidentId).ConfigureAwait(false);
            string? shelfCode = null;
            if (packageReturn.ShelfId is Guid shelfId)
            {
                var shelf = await _shelves.GetByIdAsync(shelfId).ConfigureAwait(false);
                shelfCode = shelf?.Code;
            }

            var photos = _images.FetchMulti(i => i.ReturnId == packageReturn.Id, noTracking: true).ToList();
            return PackageReturnMapper.ToDto(packageReturn, photos, package, resident, shelfCode);
        }
    }
}
