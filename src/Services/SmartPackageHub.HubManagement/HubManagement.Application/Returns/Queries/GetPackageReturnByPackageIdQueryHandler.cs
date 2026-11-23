using AutoMapper;
using HubManagement.Application.Returns.DTOs;
using HubManagement.Application.Returns.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Returns.Queries
{
    public sealed class GetPackageReturnByPackageIdQueryHandler
        : QueryHandler<GetPackageReturnByPackageIdQuery, PackageReturnDto?>
    {
        private readonly IRepository<PackageReturn> _returns;
        private readonly IRepository<PackageReturnImage> _images;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Shelf> _shelves;

        public GetPackageReturnByPackageIdQueryHandler(
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

        protected override async Task<PackageReturnDto?> HandleAsync(GetPackageReturnByPackageIdQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var rows = _returns.FetchMulti(r => r.PackageId == request.PackageId, noTracking: true)
                .AsEnumerable()
                .OrderByDescending(r => r.RequestedAtUtc)
                .ToList();

            PackageReturn? packageReturn = null;
            if (request.PreferOpen)
            {
                packageReturn = rows.FirstOrDefault(r =>
                    r.Status is PackageReturnStatus.ReturnRequested
                        or PackageReturnStatus.ReturnReceived
                        or PackageReturnStatus.ReturnShelved);
            }

            packageReturn ??= rows.FirstOrDefault();
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
