using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class CourierAgent : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; private set; }
        public string CarrierName { get; private set; } = null!; // Amazon, Flipkart, DTDC, etc.
        public string AgentName { get; private set; } = null!;
        public string Phone { get; private set; } = null!;
        public string? VehicleNumber { get; private set; }
        public string? EmployeeOrBadgeNumber { get; private set; }
        public bool IsActive { get; private set; } = true;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        private CourierAgent() { }

        private CourierAgent(Guid societyId, string carrierName, string agentName, string phone, string? vehicleNumber, string? employeeOrBadgeNumber)
        {
            SocietyId = societyId;
            CarrierName = carrierName;
            AgentName = agentName;
            Phone = phone;
            VehicleNumber = vehicleNumber;
            EmployeeOrBadgeNumber = employeeOrBadgeNumber;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public static CourierAgent Create(Guid societyId, string carrierName, string agentName, string phone, string? vehicleNumber = null, string? employeeOrBadgeNumber = null)
        {
            if (string.IsNullOrWhiteSpace(carrierName))
                throw new ArgumentException("Carrier name is required.", nameof(carrierName));
            if (string.IsNullOrWhiteSpace(agentName))
                throw new ArgumentException("Agent name is required.", nameof(agentName));
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Phone number is required.", nameof(phone));

            return new CourierAgent(societyId, carrierName.Trim(), agentName.Trim(), phone.Trim(), vehicleNumber?.Trim(), employeeOrBadgeNumber?.Trim());
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void UpdateDetails(string agentName, string phone, string? vehicleNumber, string? employeeOrBadgeNumber)
        {
            if (string.IsNullOrWhiteSpace(agentName))
                throw new ArgumentException("Agent name cannot be empty.", nameof(agentName));
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Phone number cannot be empty.", nameof(phone));

            AgentName = agentName.Trim();
            Phone = phone.Trim();
            VehicleNumber = vehicleNumber?.Trim();
            EmployeeOrBadgeNumber = employeeOrBadgeNumber?.Trim();
        }
    }
}
