namespace HubManagement.Core.Hubs.Models
{
    /// <summary>Lifecycle of a resident return request (independent of inbound intake).</summary>
    public enum PackageReturnStatus
    {
        ReturnRequested,
        ReturnReceived,
        ReturnShelved,
        ReturnPickedByCourier,
        ReturnClosed,
    }
}
