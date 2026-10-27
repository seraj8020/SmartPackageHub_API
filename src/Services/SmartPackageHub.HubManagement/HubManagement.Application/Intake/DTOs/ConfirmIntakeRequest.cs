namespace HubManagement.Application.Intake.DTOs
{
    public sealed class ConfirmIntakeRequest
    {
        public string? ClientRequestId { get; set; }
        public string? ClientPhotoId { get; set; }
        public string? Tower { get; set; }
        public string? FlatNumber { get; set; }
        public string? RecipientName { get; set; }
        public string? Courier { get; set; }
        public string? Awb { get; set; }
        public string? Barcode { get; set; }
        public string? RawOcrText { get; set; }
        public Guid? ResidentId { get; set; }
        public Guid? SocietyId { get; set; }
        public Guid? StaffUserId { get; set; }
        public string? StaffName { get; set; }
        public string? ShelfCode { get; set; }
        public string? MatchStatus { get; set; }
        public int? OcrElapsedMs { get; set; }
        public bool IsDamaged { get; set; }
        public string? DamageNote { get; set; }
        public string? Category { get; set; }
        public bool IsColdStorage { get; set; }
        public double? Weight { get; set; }
        public double? Length { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public bool IsCod { get; set; }
        public decimal? CodAmount { get; set; }
    }
}
