namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record SocietyPerformanceDto(
        Guid SocietyId,
        string SocietyName,
        int Received,
        int Delivered,
        int Pending,
        double AverageHoldDays,
        double MemberPickupRatePercent);
}
