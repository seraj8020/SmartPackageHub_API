namespace SmartPackageHub.Common.Services.Messaging.Models
{
    public sealed record EmailSendResult(bool Succeeded, string? ProviderMessageId, string? ErrorMessage);
}
