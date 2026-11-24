namespace HubManagement.Application.Intake.DTOs
{
    /// <summary>JSON body for mobile parcel/arrival photo upload (avoids multipart Content-Type issues).</summary>
    public sealed class ParcelPhotoBase64Request
    {
        public string? ClientPhotoId { get; set; }
        public string? ImageBase64 { get; set; }
        public string? ContentType { get; set; }
    }
}
