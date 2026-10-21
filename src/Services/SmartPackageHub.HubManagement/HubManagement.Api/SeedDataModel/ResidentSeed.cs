namespace HubManagement.Api.SeedDataModel
{
    internal sealed class ResidentSeed
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string TowerWing { get; set; } = string.Empty;
        public string UnitNumber { get; set; } = string.Empty;
        public bool IsMember { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
