namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record TopScannersDashboardDto(
        IReadOnlyList<TopScannerDto> Items,
        int RangeDays,
        DateOnly FromDay,
        DateOnly ToDay);
}
