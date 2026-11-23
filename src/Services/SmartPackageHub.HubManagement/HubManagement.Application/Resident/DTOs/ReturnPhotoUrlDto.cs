namespace HubManagement.Application.HubResidents.DTOs
{
    /// <summary>Lightweight refresh payload when a signed return photo URL expires.</summary>
    public sealed class ReturnPhotoUrlDto
    {
        public Guid PhotoId { get; set; }
        public Uri? PhotoUrl { get; set; }
        public DateTime? PhotoExpiresAtUtc { get; set; }
    }
}
