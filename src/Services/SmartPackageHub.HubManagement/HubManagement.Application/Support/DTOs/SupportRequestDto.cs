namespace HubManagement.Application.Support.DTOs
{
    public sealed class SupportRequestDto
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public Guid ResidentId { get; set; }
        public string ReferenceCode { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Urgency { get; set; } = "Normal";
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SupportEmail { get; set; } = string.Empty;
        public DateTime RequestedAtUtc { get; set; }
    }
}
