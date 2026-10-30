
namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record DashboardSummaryDto(
        DateOnly Date,
        int PackagesToday,
        int PendingPickup,
        int DeliveredToday,
        int ActiveSocieties,
        string ScopeLabel);
}
