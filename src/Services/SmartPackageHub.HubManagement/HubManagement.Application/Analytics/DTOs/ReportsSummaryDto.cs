namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record ReportsSummaryDto(
        int RangeDays,
        DateOnly FromDay,
        DateOnly ToDay,
        int TotalPackages,
        double AverageHoldDays,
        double MemberPickupRatePercent,
        int Received,
        int Delivered,
        int Pending);
}
