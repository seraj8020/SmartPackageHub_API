namespace SmartPackageHub.Common.Services.Messaging.Models
{
    public sealed record SmsSendRequest(
        string DestinationE164OrLocal,
        string MessageBody,
        string IdempotencyKey,
        string? SenderId = null,
        IReadOnlyDictionary<string, string>? UserDefinedFields = null);
}
