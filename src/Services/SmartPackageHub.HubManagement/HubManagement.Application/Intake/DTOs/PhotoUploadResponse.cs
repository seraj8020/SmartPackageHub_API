namespace HubManagement.Application.Intake.DTOs
{
    public sealed class PhotoUploadResponse
    {
        public string ClientPhotoId { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty;
        public bool Linked { get; set; }
    }
}
