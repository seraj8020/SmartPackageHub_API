namespace HubManagement.Application.HubResidents.DTOs
{
    public class VacationHoldRequestDto
    {
        public Guid SocietyId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
