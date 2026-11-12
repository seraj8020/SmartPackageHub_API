namespace HubManagement.Application.Couriers.DTOs
{
    public sealed class CourierBlacklistDto
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public string SocietyName { get; set; } = string.Empty;
        public string CarrierName { get; set; } = string.Empty;
        public string? AgentName { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime BlacklistedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
