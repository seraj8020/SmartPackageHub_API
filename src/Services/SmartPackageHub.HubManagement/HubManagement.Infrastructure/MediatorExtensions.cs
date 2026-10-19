using MediatR;
using SmartPackageHub.Domains.Abstractions.DomainEvents;
using SmartPackageHub.Domains.Abstractions.Entities;


namespace SmartPackageHub.HubManagement.Infrastructure
{
    internal static class MediatorExtensions
    {
        public static async Task DispatchEventsAsync(this IMediator mediator, HubDbContext context)
        {
            var aggregateRoots = context.ChangeTracker
                .Entries<EntityBase>()
                .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            var domainEvents = aggregateRoots
                .SelectMany(x => x.DomainEvents)
                .ToList();

            await mediator.DispatchDomainEventsAsync(domainEvents);

            ClearDomainEvents(aggregateRoots);
        }

        private static async Task DispatchDomainEventsAsync(this IMediator mediator, List<DomainEvent> domainEvents)
        {
            foreach (var domainEvent in domainEvents)
            {
                await mediator.Publish(domainEvent);
            }
        }

        private static void ClearDomainEvents(List<EntityBase> aggregateRoots)
        {
            aggregateRoots.ForEach(aggregate => aggregate.ClearDomainEvents());
        }
    }
}
