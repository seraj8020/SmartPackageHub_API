
namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record ReportsDailyPointDto(
        DateOnly Day,
        int Received,
        int Delivered,
        int Pending);
}
