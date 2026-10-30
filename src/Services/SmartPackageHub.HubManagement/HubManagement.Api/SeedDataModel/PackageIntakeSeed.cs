namespace HubManagement.Api.SeedDataModel
{
    internal sealed class PackageIntakeSeed
    {
        public Guid Id { get; set; }
        public Guid PackageId { get; set; }
        public Guid? CourierAgentId { get; set; }
        public DateTime ScannedAt { get; set; }
        public string MatchStatus { get; set; } = "matched";
        public string? RawOcrText { get; set; }
        public int? OcrElapsedMs { get; set; }
        public string? ClientRequestId { get; set; }
        public string? Barcode { get; set; }
        public bool IsDamaged { get; set; }
        public string? DamageNote { get; set; }
    }
}
