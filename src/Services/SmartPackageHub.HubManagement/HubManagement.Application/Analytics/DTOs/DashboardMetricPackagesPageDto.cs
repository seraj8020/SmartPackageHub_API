namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record DashboardMetricPackagesPageDto(
        IReadOnlyList<DashboardMetricPackageDto> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
