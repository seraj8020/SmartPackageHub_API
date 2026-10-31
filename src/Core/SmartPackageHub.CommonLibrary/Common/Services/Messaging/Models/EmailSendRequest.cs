namespace SmartPackageHub.Common.Services.Messaging.Models
{
    public sealed record EmailSendRequest(
        string ToAddress,
        string Subject,
        string PlainTextBody,
        string? IdempotencyKey = null);
}
