namespace HubManagement.Application.PackageHub.DTOs
{
    public sealed class StartTowerRouteRequest
    {
        public string TowerWing { get; set; } = string.Empty;
        /// <summary>When set, only shelved packages on this floor are dispatched.</summary>
        public int? Floor { get; set; }
    }
}
