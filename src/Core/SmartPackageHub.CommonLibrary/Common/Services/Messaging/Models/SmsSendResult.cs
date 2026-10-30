namespace SmartPackageHub.Common.Services.Messaging.Models
{
    public sealed record SmsSendResult(
        bool Succeeded,
        string? ProviderRequestId,
        string? ErrorMessage,
        bool IsDuplicateIgnored = false);
}
