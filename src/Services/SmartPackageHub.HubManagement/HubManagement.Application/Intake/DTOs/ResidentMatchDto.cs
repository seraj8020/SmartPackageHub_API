namespace HubManagement.Application.Intake.DTOs
{
    public sealed class ResidentMatchDto
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public string? Name { get; set; }
        public string? Tower { get; set; }
        public string? FlatNumber { get; set; }
        public Uri? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public string? PhoneNumber { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}
