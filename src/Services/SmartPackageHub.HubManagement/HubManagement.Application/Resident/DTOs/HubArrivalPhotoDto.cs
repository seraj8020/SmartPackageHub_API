namespace HubManagement.Application.HubResidents.DTOs
{
    /// <summary>
    /// Hub arrival (parcel) photo captured at intake for every package — independent of damage evidence.
    /// </summary>
    public sealed class HubArrivalPhotoDto
    {
        /// <summary>
        /// Time-limited GCS signed URL, or an authenticated API proxy path when signed URLs are unavailable (local).
        /// </summary>
        public Uri? ArrivalPhotoUrl { get; set; }

        /// <summary>UTC expiry for signed URLs. Null when the URL is an authenticated proxy path.</summary>
        public DateTime? ArrivalPhotoExpiresAtUtc { get; set; }
    }
}
