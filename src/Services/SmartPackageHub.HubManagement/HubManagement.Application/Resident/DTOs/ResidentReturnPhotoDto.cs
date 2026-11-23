namespace HubManagement.Application.HubResidents.DTOs
{
    /// <summary>Resident-uploaded return photo with a time-limited read URL (never an intake damage photo).</summary>
    public sealed class ResidentReturnPhotoDto
    {
        public Guid Id { get; set; }
        public int SortOrder { get; set; }
        public DateTime CapturedAtUtc { get; set; }
        public Uri? PhotoUrl { get; set; }
        public DateTime? PhotoExpiresAtUtc { get; set; }
    }
}
