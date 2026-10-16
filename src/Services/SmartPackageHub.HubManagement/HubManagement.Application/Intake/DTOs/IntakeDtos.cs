namespace HubManagement.Application.Intake.DTOs
{
    public sealed class ParsedLabelDto
    {
        public string? Courier { get; set; }
        public string? Awb { get; set; }
        public string? RecipientName { get; set; }
        public string? Tower { get; set; }
        public string? FlatNumber { get; set; }
    }

    public sealed class ParseLabelRequest
    {
        public string? Text { get; set; }
    }

    public sealed class ResidentMatchDto
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public string? Name { get; set; }
        public string? Tower { get; set; }
        public string? FlatNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public string? PhoneNumber { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public sealed class MatchResponse
    {
        public string Status { get; set; } = "missing";
        public List<ResidentMatchDto> Matches { get; set; } = new();
    }

    public sealed class ShelfResponse
    {
        public Guid? ShelfId { get; set; }
        public Guid? SocietyId { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
    }

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
    }

    public sealed class ConfirmIntakeResponse
    {
        public Guid IntakeId { get; set; }
        public Guid PackageId { get; set; }
        public string PkgNumber { get; set; } = string.Empty;
        public string Awb { get; set; } = string.Empty;
        public string? Courier { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public string? PickupOtp { get; set; }
        public string PickupQrPayload { get; set; } = string.Empty;
        public string NotificationStatus { get; set; } = "Queued";
        public string MatchStatus { get; set; } = "missing";
        public bool PhotoStored { get; set; }
        public ResidentMatchDto? Resident { get; set; }
    }

    public sealed class PhotoUploadResponse
    {
        public string ClientPhotoId { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty;
        public bool Linked { get; set; }
    }

    public sealed class DamageIntakeRequest
    {
        public string? ImageBase64 { get; set; }
        public string? Note { get; set; }
    }

    public sealed record IntakeStatusResult<T>(int StatusCode, string? Error, T? Body);

    public sealed record PhotoUploadResult(bool Succeeded, string? Error, PhotoUploadResponse? Body);

    public sealed record DamageSaveResult(bool Ok, string? Error);
}
