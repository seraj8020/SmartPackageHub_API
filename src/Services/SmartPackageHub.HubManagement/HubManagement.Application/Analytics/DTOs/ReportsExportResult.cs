namespace HubManagement.Application.Analytics.DTOs
{
    /// <summary>
    /// Metric definitions (UTC calendar days unless noted):
    /// - PackagesToday: packages with RecievedAt on today.
    /// - PendingPickup: packages whose Status is not Delivered.
    /// - DeliveredToday: deliveries with DeliveredAt on today.
    /// - ActiveSocieties: distinct societies in scope (platform = all; society = 1).
    /// - AvgHoldDays: average (DeliveredAt − RecievedAt) in days for deliveries in range.
    /// - MemberPickupRate: deliveredInRange / receivedInRange × 100 (0 if none received).
    /// </summary>
    
    public sealed record ReportsExportResult(
        string FileName,
        string ContentType,
        string CsvContent);
}
