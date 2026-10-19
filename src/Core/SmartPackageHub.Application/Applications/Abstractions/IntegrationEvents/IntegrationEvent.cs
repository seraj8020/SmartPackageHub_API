using Newtonsoft.Json;

namespace SmartPackageHub.Applications.Abstractions.IntegrationEvents
{
    public class IntegrationEvent
    {
        public IntegrationEvent(Guid messageId)
        {
            MessageId = messageId;
            CreationDate = DateTime.UtcNow;
        }

        [JsonConstructor]
        public IntegrationEvent(Guid messageId, DateTime createDate)
        {
            MessageId = messageId;
            CreationDate = createDate;
        }

        [JsonProperty]
        public Guid MessageId { get; private set; }

        [JsonProperty]
        public DateTime CreationDate { get; private set; }
    }
}
