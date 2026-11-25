namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed class AuthorizedCollectorDto
    {
        public Guid Id { get; set; }
        public Guid PackageId { get; set; }
        public string CollectorName { get; set; } = string.Empty;
        public string? CollectorPhone { get; set; }
        public string? Note { get; set; }
        public DateTime AuthorizedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public bool IsActive { get; set; }
    }
}
