namespace SmartPackageHub.Common.Services.Messaging.Options
{
    public sealed class OutboundMessageQueueOptions
    {
        public const string SectionName = "OutboundMessageQueue";

        public string RedisListKey { get; set; } = "sph:outbound-messages";

        public int DequeuePollMilliseconds { get; set; } = 500;
    }
}
