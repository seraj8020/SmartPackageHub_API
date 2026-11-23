namespace HubManagement.Application.HubResidents.DTOs
{
    /// <summary>Lightweight refresh payload when a signed damage photo URL expires.</summary>
    public sealed class DamagePhotoUrlDto
    {
        public Uri? DamagePhotoUrl { get; set; }
        public DateTime? DamagePhotoExpiresAtUtc { get; set; }
    }
}
