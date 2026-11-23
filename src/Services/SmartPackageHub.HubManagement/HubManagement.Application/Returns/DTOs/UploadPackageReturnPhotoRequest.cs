namespace HubManagement.Application.Returns.DTOs
{
    public sealed class UploadPackageReturnPhotoRequest
    {
        public string ImageBase64 { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
