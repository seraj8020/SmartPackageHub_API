using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class Notification : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; private set; }
        public Guid PackageId { get; private set; }
        public Guid ResidentId { get; private set; }
        public NotificationChannel Channel { get; private set; } = null!;
        public MessageContent Content { get; private set; } = null!;
        public NotificationStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? SentAt { get; private set; }
        public string? FailureReason { get; private set; }

        // Encapsulated constructor
        private Notification() { }
        private Notification(Guid societyId, Guid residentId, Guid packageId, NotificationChannel channel, MessageContent content)
        {
            SocietyId = societyId;
            ResidentId = residentId;
            PackageId = packageId;
            Channel = channel;
            Content = content;
            Status = NotificationStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            FailureReason = string.Empty;
        }

        // Factory method for creating new notifications
        public static Notification Create(Guid societyId, Guid residentId, Guid packageId, NotificationChannel channel, MessageContent content)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            return new Notification(societyId, residentId, packageId, channel, content);
        }

        // Domain Behaviors
        public void MarkAsSent()
        {
            Status = NotificationStatus.Sent;
            SentAt = DateTime.UtcNow;
            FailureReason = string.Empty;
        }

        public void MarkAsFailed(string reason)
        {
            Status = NotificationStatus.Failed;
            FailureReason = reason;
        }
    }
}
