namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed class ResidentProfileDto
    {
        public Guid Id { get; init; }
        public Guid SocietyId { get; init; }
        public string? FullName { get; init; }
        public string? PhoneNumber { get; init; }
        public string? TowerWing { get; init; }
        public string? FlatNumber { get; init; }
    }
}
