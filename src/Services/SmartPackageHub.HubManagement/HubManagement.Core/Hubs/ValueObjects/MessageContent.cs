using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class MessageContent: ValueObject
    {
        public string TemplateName { get; private set; }
        public string Body { get; private set; }
        
        private MessageContent(string templateName, string body)
        {
            TemplateName = templateName;
            Body = body;
        }
        public static MessageContent Create(string templateName, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Message body cannot be empty.", nameof(body));
            string newTemplateName = string.Empty;
            newTemplateName = templateName ?? "None";
            return new MessageContent(newTemplateName, body);
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return TemplateName;
            yield return Body;
        }
    }
}
