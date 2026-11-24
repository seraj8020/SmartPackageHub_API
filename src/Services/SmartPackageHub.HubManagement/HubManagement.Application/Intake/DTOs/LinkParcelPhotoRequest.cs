namespace HubManagement.Application.Intake.DTOs
{
    /// <summary>Re-link a previously uploaded arrival photo to an intake without re-uploading bytes.</summary>
    public sealed class LinkParcelPhotoRequest
    {
        public string? ClientPhotoId { get; set; }
        public Guid? IntakeId { get; set; }
        public Guid? PackageId { get; set; }
    }
}
