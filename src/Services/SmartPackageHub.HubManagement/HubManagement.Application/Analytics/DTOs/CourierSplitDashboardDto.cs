namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record CourierSplitDashboardDto(
        IReadOnlyList<CourierSplitSliceDto> Items,
        DateTime? AsOfUtc);
}
