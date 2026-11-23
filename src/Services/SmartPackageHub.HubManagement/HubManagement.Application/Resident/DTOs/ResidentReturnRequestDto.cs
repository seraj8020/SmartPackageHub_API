namespace HubManagement.Application.HubResidents.DTOs
{
    /// <summary>
    /// "Your return request" section — independent of hub intake damage.
    /// </summary>
    public sealed class ResidentReturnRequestDto
    {
        public Guid Id { get; set; }
        public Guid PackageId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public IReadOnlyList<ResidentReturnPhotoDto> Photos { get; set; } = Array.Empty<ResidentReturnPhotoDto>();
    }
}
