namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record DashboardMetricPackageDto(
        Guid Id,
        string PackageNumber,
        string RecipientName,
        DateTime ReceivedAtUtc,
        DateTime? DeliveredAtUtc,
        string? Courier,
        string? TowerWing,
        string FlatNumber,
        string Status,
        Guid SocietyId,
        string? SocietyName);
}
