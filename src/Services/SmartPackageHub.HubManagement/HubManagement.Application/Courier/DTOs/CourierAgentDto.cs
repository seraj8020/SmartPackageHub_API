namespace HubManagement.Application.Couriers.DTOs
{
    public sealed class CourierAgentDto
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public string SocietyName { get; set; } = string.Empty;
        public string CarrierName { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? VehicleNumber { get; set; }
        public string? EmployeeOrBadgeNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsBlacklisted { get; set; }
        public string? BlacklistReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
