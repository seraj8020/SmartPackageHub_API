using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class CourierAgent : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; private set; }
        public string CarrierName { get; private set; } = null!; // Amazon, Flipkart, DTDC, etc.
        public string? AgentName { get; private set; }
        public string? Phone { get; private set; }
        public string? VehicleNumber { get; private set; }
        public string? EmployeeOrBadgeNumber { get; private set; }
        public bool IsActive { get; private set; } = true;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        private CourierAgent() { }

        private CourierAgent(Guid societyId, string carrierName, string? agentName, string? phone, string? vehicleNumber, string? employeeOrBadgeNumber)
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

        public static CourierAgent Create(
            Guid societyId,
            string carrierName,
            string? agentName = null,
            string? phone = null,
            string? vehicleNumber = null,
            string? employeeOrBadgeNumber = null)
        {
            if (string.IsNullOrWhiteSpace(carrierName))
                throw new ArgumentException("Carrier name is required.", nameof(carrierName));

            return new CourierAgent(
                societyId,
                carrierName.Trim(),
                NullIfWhiteSpace(agentName),
                NullIfWhiteSpace(phone),
                NullIfWhiteSpace(vehicleNumber),
                NullIfWhiteSpace(employeeOrBadgeNumber));
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void UpdateDetails(
            string carrierName,
            string? agentName,
            string? phone,
            string? vehicleNumber,
            string? employeeOrBadgeNumber)
        {
            if (string.IsNullOrWhiteSpace(carrierName))
                throw new ArgumentException("Carrier name cannot be empty.", nameof(carrierName));

            CarrierName = carrierName.Trim();
            AgentName = NullIfWhiteSpace(agentName);
            Phone = NullIfWhiteSpace(phone);
            VehicleNumber = NullIfWhiteSpace(vehicleNumber);
            EmployeeOrBadgeNumber = NullIfWhiteSpace(employeeOrBadgeNumber);
        }

        private static string? NullIfWhiteSpace(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
