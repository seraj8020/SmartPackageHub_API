using MediatR;

namespace SmartPackageHub.Domains.Abstractions.DomainEvents
{
#pragma warning disable CA1040 // Domain events are identified by this marker and INotification.
    public interface IDomainEvent : INotification
    {
    }
#pragma warning restore CA1040
}
