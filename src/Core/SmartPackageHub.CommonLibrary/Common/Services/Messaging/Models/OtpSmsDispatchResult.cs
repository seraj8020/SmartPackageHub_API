namespace SmartPackageHub.Common.Services.Messaging.Models
{
    /// <summary>
    /// Whether queue succeeded, notification id, idempotency key, error message.
    /// </summary>
    /// <param name="Queued"></param>
    /// <param name="NotificationId"></param>
    /// <param name="IdempotencyKey"></param>
    /// <param name="ErrorMessage"></param>
    public sealed record OtpSmsDispatchResult(
        bool Queued,
        Guid? NotificationId,
        string? IdempotencyKey,
        string? ErrorMessage);
}
