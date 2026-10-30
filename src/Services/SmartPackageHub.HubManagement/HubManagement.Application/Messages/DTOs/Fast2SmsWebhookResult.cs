namespace HubManagement.Application.Messages.DTOs
{
    public sealed record Fast2SmsWebhookResult(
        bool Received,
        bool Matched,
        Guid? NotificationId = null);
}
