namespace HubManagement.Application.Analytics.DTOs
{
    public sealed class PackageAuditTimelineDto
    {
        public AuditPackageSearchItemDto Package { get; set; } = new();
        public IReadOnlyList<RecentActivityItemDto> Events { get; set; } = Array.Empty<RecentActivityItemDto>();
    }
}