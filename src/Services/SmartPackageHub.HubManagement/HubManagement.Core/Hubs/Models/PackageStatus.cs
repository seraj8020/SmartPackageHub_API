namespace HubManagement.Core.Hubs.Models
{
    public enum PackageStatus
    {
        Received,
        Shelved,
        Dispatched,//For Handover
        Delivered,

        // Return lifecycle (mirrors PackageReturnStatus for package list filters).
        ReturnRequested,
        ReturnReceived,
        ReturnShelved,
        ReturnPickedByCourier,
        ReturnClosed,
    }
}
