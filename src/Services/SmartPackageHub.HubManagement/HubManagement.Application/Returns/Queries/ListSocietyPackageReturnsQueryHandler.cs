using AutoMapper;
using HubManagement.Application.Returns.DTOs;
using HubManagement.Application.Returns.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Returns.Queries
{
    public sealed class ListSocietyPackageReturnsQueryHandler
        : QueryHandler<ListSocietyPackageReturnsQuery, IReadOnlyList<PackageReturnDto>>
    {
        private readonly IRepository<PackageReturn> _returns;
        private readonly IRepository<PackageReturnImage> _images;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Shelf> _shelves;

        public ListSocietyPackageReturnsQueryHandler(
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

        protected override Task<IReadOnlyList<PackageReturnDto>> HandleAsync(ListSocietyPackageReturnsQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var returns = _returns.FetchMulti(r => r.SocietyId == request.SocietyId, noTracking: true)
                .AsEnumerable()
                .Where(r => !request.OpenOnly ||
                            r.Status is PackageReturnStatus.ReturnRequested
                                or PackageReturnStatus.ReturnReceived
                                or PackageReturnStatus.ReturnShelved)
                .OrderByDescending(r => r.RequestedAtUtc)
                .ToList();

            if (returns.Count == 0)
                return Task.FromResult<IReadOnlyList<PackageReturnDto>>(Array.Empty<PackageReturnDto>());

            var packageIds = returns.Select(r => r.PackageId).Distinct().ToList();
            var residentIds = returns.Select(r => r.ResidentId).Distinct().ToList();
            var returnIds = returns.Select(r => r.Id).ToList();
            var shelfIds = returns.Where(r => r.ShelfId.HasValue).Select(r => r.ShelfId!.Value).Distinct().ToList();

            var packages = _packages.FetchMulti(p => packageIds.Contains(p.Id), noTracking: true)
                .ToDictionary(p => p.Id);
            var residents = _residents.FetchMulti(r => residentIds.Contains(r.Id), noTracking: true)
                .ToDictionary(r => r.Id);
            var shelves = shelfIds.Count == 0
                ? new Dictionary<Guid, Shelf>()
                : _shelves.FetchMulti(s => shelfIds.Contains(s.Id), noTracking: true)
                    .ToDictionary(s => s.Id);
            var photosByReturn = _images.FetchMulti(i => returnIds.Contains(i.ReturnId), noTracking: true)
                .GroupBy(i => i.ReturnId)
                .ToDictionary(g => g.Key, g => (IEnumerable<PackageReturnImage>)g.ToList());

            IReadOnlyList<PackageReturnDto> result = returns.Select(r =>
            {
                packages.TryGetValue(r.PackageId, out var package);
                residents.TryGetValue(r.ResidentId, out var resident);
                string? shelfCode = null;
                if (r.ShelfId is Guid shelfId && shelves.TryGetValue(shelfId, out var shelf))
                    shelfCode = shelf.Code;
                photosByReturn.TryGetValue(r.Id, out var photos);
                return PackageReturnMapper.ToDto(r, photos, package, resident, shelfCode);
            }).ToList();

            return Task.FromResult(result);
        }
    }
}
