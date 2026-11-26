namespace HubManagement.Application.Support.DTOs
{
    public sealed class SupportRequestDto
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public Guid ResidentId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SupportEmail { get; set; } = string.Empty;
        public DateTime RequestedAtUtc { get; set; }
    }
}
