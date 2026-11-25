namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed class AuthorizeCollectorRequestDto
    {
        public string CollectorName { get; set; } = string.Empty;
        public string? CollectorPhone { get; set; }
        public string? Note { get; set; }
        /// <summary>Optional UTC expiry. Defaults to 7 days when omitted.</summary>
        public DateTime? ExpiresAtUtc { get; set; }
    }
}
