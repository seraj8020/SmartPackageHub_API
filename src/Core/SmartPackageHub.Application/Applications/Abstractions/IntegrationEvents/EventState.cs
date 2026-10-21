namespace SmartPackageHub.Applications.Abstractions.IntegrationEvents
{
    public enum EventState
    {
        None = 0,
        ReadyToPublish = 1,
        InProgress = 2,
        Published = 3,
        PublishedFailed = 4
    }
}
