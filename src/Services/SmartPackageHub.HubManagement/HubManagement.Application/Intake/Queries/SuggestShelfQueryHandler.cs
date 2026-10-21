using AutoMapper;
using HubManagement.Application.Intake.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Intake.Queries
{
    public sealed class SuggestShelfQueryHandler : QueryHandler<SuggestShelfQuery, ShelfResponse?>
    {
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<Package> _packages;

        public SuggestShelfQueryHandler(IMapper mapper, IRepository<Shelf> shelves, IRepository<Package> packages) : base(mapper)
        {
            _shelves = shelves;
            _packages = packages;
        }

        protected override Task<ShelfResponse?> HandleAsync(SuggestShelfQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var shelf = ShelfSuggester.Suggest(_shelves.GetAll().ToList(), _packages.GetAll().ToList(), request.SocietyId);
            if (shelf == null)
                return Task.FromResult<ShelfResponse?>(null);

            return Task.FromResult<ShelfResponse?>(new ShelfResponse
            {
                ShelfId = shelf.Id,
                SocietyId = shelf.SocietyId,
                ShelfCode = shelf.Code
            });
        }
    }
}
