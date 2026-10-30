using AutoMapper;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Queries
{
    public sealed class GetShelvesBySocietyIdQueryHandler : QueryHandler<GetShelvesBySocietyIdQuery, List<SocietyShelfDto>?>
    {
        private readonly IRepository<Society> _societies;
        private readonly IRepository<Shelf> _shelves;

        public GetShelvesBySocietyIdQueryHandler(
            IMapper mapper,
            IRepository<Society> societies,
            IRepository<Shelf> shelves) : base(mapper)
        {
            _societies = societies;
            _shelves = shelves;
        }

        protected override async Task<List<SocietyShelfDto>?> HandleAsync(GetShelvesBySocietyIdQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var society = await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            if (society == null)
                return null;

            var shelves = await _shelves.ListAsync(
                shelf => shelf.SocietyId == society.Id,
                noTracking: true).ConfigureAwait(false);

            return shelves
                .OrderBy(shelf => shelf.Code, StringComparer.OrdinalIgnoreCase)
                .Select(shelf => new SocietyShelfDto
                {
                    Id = shelf.Id,
                    Code = shelf.Code,
                    TowerOrWing = shelf.TowerOrWing,
                    IsColdStorage = shelf.IsColdStorage,
                    Capacity = shelf.Capacity,
                })
                .ToList();
        }
    }
}
