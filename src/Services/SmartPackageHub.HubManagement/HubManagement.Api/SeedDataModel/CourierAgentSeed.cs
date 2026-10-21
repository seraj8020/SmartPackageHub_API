namespace HubManagement.Api.SeedDataModel
{
    internal sealed class CourierAgentSeed
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public string CarrierName { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? VehicleNumber { get; set; }
        public string? EmployeeOrBadgeNumber { get; set; }
    }
}







