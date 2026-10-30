namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record CourierSplitSliceDto(
        string Courier,
        int Count,
        double Percent);
}
