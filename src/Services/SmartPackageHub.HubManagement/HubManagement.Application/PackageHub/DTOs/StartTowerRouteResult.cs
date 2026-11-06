namespace HubManagement.Application.PackageHub.DTOs
{
    public sealed record StartTowerRouteResult(
        bool SocietyFound,
        bool Succeeded,
        string Message,
        int DispatchedCount,
        IReadOnlyList<Guid> PackageIds);
}
