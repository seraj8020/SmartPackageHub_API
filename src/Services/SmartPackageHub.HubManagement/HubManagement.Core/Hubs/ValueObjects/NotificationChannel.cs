using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class NotificationChannel : ValueObject
    {
        public ChannelType Type { get; private set; }
        public string Destination { get; private set; } // Phone number, Device Token, or Email address

        private NotificationChannel(ChannelType type, string destination)
        {
            Type = type;
            Destination = destination;
        }
        public static NotificationChannel Create(ChannelType type, string destination)
        {
            if (string.IsNullOrWhiteSpace(destination))
                throw new ArgumentException("Destination cannot be empty.", nameof(destination));
            return new NotificationChannel(type, destination);
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Type;
            yield return Destination;
        }
    }
}
