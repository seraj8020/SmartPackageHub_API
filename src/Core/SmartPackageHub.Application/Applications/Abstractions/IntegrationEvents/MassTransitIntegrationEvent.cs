//using SmartPackageHub.Domains.Abstractions.Models;

namespace SmartPackageHub.Applications.Abstractions.IntegrationEvents
{
    public abstract record MassTransitIntegrationEvent(string CorrelationId);
}
