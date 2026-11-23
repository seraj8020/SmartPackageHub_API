namespace HubManagement.Application.Analytics.DTOs
{
    public sealed class AuditPackageSearchPageDto
    {
        public IReadOnlyList<AuditPackageSearchItemDto> Items { get; set; } = Array.Empty<AuditPackageSearchItemDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
