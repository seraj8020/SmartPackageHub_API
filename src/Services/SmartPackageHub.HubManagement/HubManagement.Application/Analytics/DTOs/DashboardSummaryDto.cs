
namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record DashboardSummaryDto(
        DateOnly Date,
        int PackagesToday,
        int PendingPickup,
        int DeliveredToday,
        int ActiveSocieties,
        string ScopeLabel,
        DateTime? LastRollupAtUtc,
        int SocietiesRolledUp,
        int PackagesLast7Days = 0,
        int DeliveredLast7Days = 0,
        int ReturnedToday = 0,
        int ReturnedLast7Days = 0);
}
