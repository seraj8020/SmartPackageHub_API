using AutoMapper;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Queries
{
    public sealed class GetShelfInventoryQueryHandler : QueryHandler<GetShelfInventoryQuery, List<ShelfInventoryDto>?>
    {
        private readonly IRepository<Society> _societies;
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<Package> _packages;

        public GetShelfInventoryQueryHandler(IMapper mapper, IRepository<Society> societies, IRepository<Shelf> shelves, IRepository<Package> packages) : base(mapper)
        {
            _societies = societies;
            _shelves = shelves;
            _packages = packages;
        }

        protected override async Task<List<ShelfInventoryDto>?> HandleAsync(GetShelfInventoryQuery request)
        {
            if (await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false) is null)
                return null;

            var shelves = await _shelves.ListAsync(shelf => shelf.SocietyId == request.SocietyId, noTracking: true).ConfigureAwait(false);
            var packages = await _packages.ListAsync(package =>
                package.SocietyId == request.SocietyId &&
                package.ShelfId != null &&
                package.Status != PackageStatus.Delivered,
                noTracking: true).ConfigureAwait(false);

            var packagesByShelf = packages.GroupBy(package => package.ShelfId!.Value)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(package => package.RecievedAt).ToList());

            return shelves.OrderBy(shelf => shelf.Code, StringComparer.OrdinalIgnoreCase).Select(shelf =>
            {
                var stored = packagesByShelf.GetValueOrDefault(shelf.Id) ?? [];
                return new ShelfInventoryDto
                {
                    Id = shelf.Id,
                    Code = shelf.Code,
                    Capacity = shelf.Capacity,
                    PackageCount = stored.Count,
                    IsFull = shelf.Capacity > 0 && stored.Count >= shelf.Capacity,
                    IsColdStorage = shelf.IsColdStorage,
                    Packages = stored.Select(package => new ShelfInventoryPackageDto
                    {
                        Id = package.Id,
                        ResidentName = package.RecipientName,
                        PackageNumber = package.PkgNumber,
                        Courier = package.CourierPartner,
                        ReceivedAt = package.RecievedAt,
                    }).ToList(),
                };
            }).ToList();
        }
    }
}
