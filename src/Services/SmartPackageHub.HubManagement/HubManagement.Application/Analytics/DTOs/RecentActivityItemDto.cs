
namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record RecentActivityItemDto(
       DateTime OccurredAtUtc,
       string Kind,
       string Text,
       Guid? SocietyId,
       string? SocietyName,
       Guid? PackageId);
}
