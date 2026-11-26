namespace HubManagement.Application.Support.DTOs
{
    public sealed class CreateSupportRequestDto
    {
        public Guid ResidentId { get; set; }
        public Guid SocietyId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ResidentName { get; set; }
        public string? ResidentPhone { get; set; }
        public string? TowerWing { get; set; }
        public string? FlatNumber { get; set; }
    }
}
