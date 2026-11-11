namespace SmartPackageHub.HubManagement.Api.Controllers
{
    public sealed class UpdateResidentRequest
    {
        public string Username { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string TowerWing { get; set; } = string.Empty;
        public string UnitNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid? SocietyId { get; set; }
    }
}
