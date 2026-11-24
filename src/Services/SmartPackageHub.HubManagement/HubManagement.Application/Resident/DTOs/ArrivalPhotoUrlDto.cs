namespace HubManagement.Application.HubResidents.DTOs
{
    /// <summary>Lightweight refresh payload when a signed arrival photo URL expires.</summary>
    public sealed class ArrivalPhotoUrlDto
    {
        public Uri? ArrivalPhotoUrl { get; set; }
        public DateTime? ArrivalPhotoExpiresAtUtc { get; set; }
    }
}
