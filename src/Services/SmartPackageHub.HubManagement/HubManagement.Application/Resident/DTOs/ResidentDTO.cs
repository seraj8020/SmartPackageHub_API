using HubManagement.Core.Hubs.ValueObjects;

namespace HubManagement.Application.HubResidents.DTOs
{
    public class ResidentDto
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? TowerWing { get; set; }
        public UnitNumber? Unit { get; set; } //flat number
    }
}
