
namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record RecentActivityItemDto(
       DateTime OccurredAtUtc,
       string Kind,
       string Text,
       Guid? SocietyId,
       string? SocietyName,
       Guid? PackageId,
       string? FromZone = null,
       string? ToZone = null,
       string? LocationLabel = null,
       string? StaffUsername = null,
       string? PackageNumber = null);
}
