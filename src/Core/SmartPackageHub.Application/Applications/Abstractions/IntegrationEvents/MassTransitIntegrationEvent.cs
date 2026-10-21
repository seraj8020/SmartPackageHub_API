namespace SmartPackageHub.Applications.Abstractions.IntegrationEvents
{
    public abstract record MassTransitIntegrationEvent(string CorrelationId);
}
