namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed class VacationHoldDto
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public Guid ResidentId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string? TowerWing { get; set; }
        public string? FlatNumber { get; set; }
    }
}
