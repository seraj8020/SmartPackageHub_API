namespace HubManagement.Application.Returns.DTOs
{
    public sealed class PackageReturnImageDto
    {
        public Guid Id { get; set; }
        public string StorageKey { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public DateTime CapturedAtUtc { get; set; }
    }
}
