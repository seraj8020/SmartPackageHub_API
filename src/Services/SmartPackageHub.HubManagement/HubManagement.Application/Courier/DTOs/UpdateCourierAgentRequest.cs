namespace HubManagement.Application.Couriers.DTOs
{
    public sealed class UpdateCourierAgentRequest
    {
        public string? CarrierName { get; set; }
        public string? AgentName { get; set; }
        public string? Phone { get; set; }
        public string? VehicleNumber { get; set; }
        public string? EmployeeOrBadgeNumber { get; set; }
        public bool? IsActive { get; set; }
    }
}
