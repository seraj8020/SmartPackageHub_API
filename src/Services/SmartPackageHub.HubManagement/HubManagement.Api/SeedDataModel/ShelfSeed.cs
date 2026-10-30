namespace HubManagement.Api.SeedDataModel
{
    internal sealed class ShelfSeed
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string TowerOrWing { get; set; } = string.Empty;
        public bool IsColdStorage { get; set; }
        public int Capacity { get; set; }
        public string Zone { get; set; } = string.Empty;
        public string Aisle { get; set; } = string.Empty;
        public int Tier { get; set; }
    }
}
