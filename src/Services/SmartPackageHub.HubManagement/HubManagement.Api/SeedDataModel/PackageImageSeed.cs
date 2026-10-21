namespace HubManagement.Api.SeedDataModel
{
    internal sealed class PackageImageSeed
    {
        public Guid Id { get; set; }
        public Guid PackageId { get; set; }
        public Guid IntakeId { get; set; }
        public string Kind { get; set; } = "label";
        public string StorageKey { get; set; } = string.Empty;
        public string Sha256Hash { get; set; } = string.Empty;
        public string ContentType { get; set; } = "image/jpeg";
        public long ByteSize { get; set; }
        public DateTime CapturedAt { get; set; }
    }
}
