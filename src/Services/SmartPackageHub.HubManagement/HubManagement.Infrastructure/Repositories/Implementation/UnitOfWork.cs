using MediatR;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Domains.Abstractions.DomainEvents;
using SmartPackageHub.Domains.Abstractions.Entities;

namespace SmartPackageHub.HubManagement.Infrastructure.Repositories
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly HubDbContext _context;
        private readonly IMediator _mediator;

        public UnitOfWork(HubDbContext context,
            IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<bool> CommitAsync(CancellationToken cancellationToken = default)
        {
            // Dispatch Domain Events collection. 
            // Right BEFORE committing data (EF SaveChanges) into the DB will make a single transaction including  
            // side effects from the domain event handlers which are using the same DbContext with "InstancePerLifetimeScope" or "scoped" lifetime
            // Integration Events will be stored in the IntegrationEventOutbox ready to be published later
            await DispatchEventsAsync().ConfigureAwait(false);

            // After executing this line all the changes (from any Command Handler and Domain Event Handlers) 
            // performed through the DbContext will be committed
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return true;
        }

        private async Task DispatchEventsAsync()
        {
            var processedDomainEvents = new List<DomainEvent>();
            var unprocessedDomainEvents = GetDomainEvents();
            // this is needed incase another DomainEvent is published from a DomainEventHandler
            while (unprocessedDomainEvents.Count > 0)
            {
                await DispatchDomainEventsAsync(unprocessedDomainEvents).ConfigureAwait(false);
                processedDomainEvents.AddRange(unprocessedDomainEvents);
                unprocessedDomainEvents = GetDomainEvents()
                                            .Where(e => !processedDomainEvents.Contains(e))
                                            .ToList();
            }

            ClearDomainEvents();
        }

        private List<DomainEvent> GetDomainEvents()
        {
            var aggregateRoots = GetTrackedAggregateRoots();
            return aggregateRoots
                .SelectMany(x => x.DomainEvents)
                .ToList();
        }

        private List<EntityBase> GetTrackedAggregateRoots()
        {
            return _context.ChangeTracker
                .Entries<EntityBase>()
                .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();
        }

        private async Task DispatchDomainEventsAsync(List<DomainEvent> domainEvents)
        {
            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent).ConfigureAwait(false);
            }
        }

        private void ClearDomainEvents()
        {
            var aggregateRoots = GetTrackedAggregateRoots();
            aggregateRoots.ForEach(aggregate => aggregate.ClearDomainEvents());
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
