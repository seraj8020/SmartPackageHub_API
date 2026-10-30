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
        public DateTime? DeliveredAt { get; private set; }
        public string? FailureReason { get; private set; }
        public string? IdempotencyKey { get; private set; }
        public string? ProviderRequestId { get; private set; }
        public Guid? FallbackOfNotificationId { get; private set; }
        public int SegmentCount { get; private set; }

        private Notification() { }

        private Notification(
            Guid societyId,
            Guid residentId,
            Guid packageId,
            NotificationChannel channel,
            MessageContent content,
            string? idempotencyKey,
            Guid? fallbackOfNotificationId,
            int segmentCount)
        {
            SocietyId = societyId;
            ResidentId = residentId;
            PackageId = packageId;
            Channel = channel;
            Content = content;
            Status = NotificationStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            FailureReason = string.Empty;
            IdempotencyKey = idempotencyKey;
            FallbackOfNotificationId = fallbackOfNotificationId;
            SegmentCount = segmentCount < 1 ? 1 : segmentCount;
        }

        /// <summary>
        /// pending notification with idempotency/segments/fallback link
        /// </summary>
        /// <param name="societyId"></param>
        /// <param name="residentId"></param>
        /// <param name="packageId"></param>
        /// <param name="channel"></param>
        /// <param name="content"></param>
        /// <param name="idempotencyKey"></param>
        /// <param name="fallbackOfNotificationId"></param>
        /// <param name="segmentCount"></param>
        /// <returns></returns>
        public static Notification Create(
            Guid societyId,
            Guid residentId,
            Guid packageId,
            NotificationChannel channel,
            MessageContent content,
            string? idempotencyKey = null,
            Guid? fallbackOfNotificationId = null,
            int segmentCount = 1)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(content);
            return new Notification(
                societyId,
                residentId,
                packageId,
                channel,
                content,
                idempotencyKey,
                fallbackOfNotificationId,
                segmentCount);
        }

        /// <summary>
        /// legacy “sent” without provider id
        /// </summary>
        public void MarkAsSent()
        {
            Status = NotificationStatus.Sent;
            SentAt = DateTime.UtcNow;
            FailureReason = string.Empty;
        }

        /// <summary>
        /// accepted by provider (ProviderRequestId)
        /// </summary>
        /// <param name="providerRequestId"></param>
        public void MarkAsSubmitted(string providerRequestId)
        {
            ProviderRequestId = providerRequestId;
            Status = NotificationStatus.Sent;
            SentAt = DateTime.UtcNow;
            FailureReason = string.Empty;
        }

        /// <summary>
        /// webhook confirmed delivery
        /// </summary>
        public void MarkAsDelivered()
        {
            Status = NotificationStatus.Sent;
            DeliveredAt = DateTime.UtcNow;
            FailureReason = string.Empty;
        }

        /// <summary>
        /// send or delivery failure
        /// </summary>
        /// <param name="reason"></param>
        public void MarkAsFailed(string reason)
        {
            Status = NotificationStatus.Failed;
            FailureReason = reason;
        }
    }
}
