
namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record WeeklyTrendPointDto(
        DateOnly Day,
        string Label,
        int Received,
        int Delivered,
        int Pending);
}
