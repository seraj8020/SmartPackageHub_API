namespace HubManagement.Application.PackageHub.DTOs
{
    public sealed record MarkTowerFloorArrivalResult(
        bool SocietyFound,
        bool Succeeded,
        string Message,
        int LoggedCount,
        IReadOnlyList<Guid> PackageIds);
}
