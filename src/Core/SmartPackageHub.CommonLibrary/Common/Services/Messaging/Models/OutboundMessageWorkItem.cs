
namespace SmartPackageHub.Common.Services.Messaging.Models
{
    /// <summary>
    /// Queue payload: notification id, channel (SMS/WhatsApp), idempotency key, enqueue time.
    /// </summary>
    /// <param name="NotificationId"></param>
    /// <param name="Channel"></param>
    /// <param name="IdempotencyKey"></param>
    /// <param name="EnqueuedAtUtc"></param>
    public sealed record OutboundMessageWorkItem(
        Guid NotificationId,
        string Channel,
        string IdempotencyKey,
        DateTime EnqueuedAtUtc);
}
