using SmartPackageHub.Domains.Abstractions.DomainEvents;

namespace SmartPackageHub.Domains.Abstractions.Entities
{
    public abstract class EntityBase : Entity<Guid>
    {
        protected EntityBase() : this(Guid.NewGuid())
        {

        }
        protected EntityBase(Guid id)
        {
            Id = id;
        }
        private readonly List<DomainEvent> _domainEvents = new List<DomainEvent>();
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(DomainEvent eventItem)
        {
            _domainEvents.Add(eventItem);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
