namespace HubManagement.Core.Hubs.Models
{
    /// <summary>Stable kind strings for internal package chain-of-custody hops.</summary>
    public static class PackageCustodyHopKinds
    {
        public const string ReceivedAtHub = "received-at-hub";
        public const string Shelved = "shelved";
        public const string ZoneTransfer = "zone-transfer";
        public const string OutForDoorDelivery = "out-for-door";
        /// <summary>Runner arrived at a tower (and optional floor) during a door-delivery route.</summary>
        public const string ArrivedAtTowerFloor = "arrived-at-tower-floor";
        public const string Delivered = "delivered";
        public const string DeliveryFailed = "delivery-failed";
        public const string ReturnedToCage = "returned-to-cage";

        public const string DefaultHubZone = "Hub";
    }
}
