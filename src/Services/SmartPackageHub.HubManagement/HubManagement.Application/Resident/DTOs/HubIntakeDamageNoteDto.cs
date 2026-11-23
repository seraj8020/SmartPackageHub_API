namespace HubManagement.Application.HubResidents.DTOs
{
    /// <summary>
    /// Hub intake damage details for a single package. Loaded only on detail/refresh — never on list/history.
    /// </summary>
    public sealed class HubIntakeDamageNoteDto
    {
        public string? DamageNote { get; set; }

        /// <summary>
        /// Time-limited GCS signed URL, or an authenticated API proxy path when signed URLs are unavailable (local).
        /// </summary>
        public Uri? DamagePhotoUrl { get; set; }

        /// <summary>UTC expiry for signed URLs. Null when the URL is an authenticated proxy path.</summary>
        public DateTime? DamagePhotoExpiresAtUtc { get; set; }
    }
}
