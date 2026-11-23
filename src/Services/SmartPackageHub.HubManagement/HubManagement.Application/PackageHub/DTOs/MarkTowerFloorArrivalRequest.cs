namespace HubManagement.Application.PackageHub.DTOs
{
    /// <summary>
    /// Door-delivery waypoint: runner arrived at a tower (and optional floor). Not used for hub counter.
    /// </summary>
    public sealed class MarkTowerFloorArrivalRequest
    {
        public string TowerWing { get; set; } = string.Empty;
        public int? Floor { get; set; }
    }
}
