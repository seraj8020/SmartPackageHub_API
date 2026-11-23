namespace HubManagement.Application.Returns.DTOs
{
    public sealed class PackageReturnDto
    {
        public Guid Id { get; set; }
        public Guid PackageId { get; set; }
        public Guid SocietyId { get; set; }
        public Guid ResidentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public DateTime? ReceivedAtUtc { get; set; }
        public Guid? ReceivedByStaffUserId { get; set; }
        public Guid? ShelfId { get; set; }
        public string? ShelfCode { get; set; }
        public DateTime? ShelfAssignedAtUtc { get; set; }
        public string? CourierPickupNote { get; set; }
        public string? CourierPickupAwb { get; set; }
        public DateTime? ClosedAtUtc { get; set; }
        public string? PkgNumber { get; set; }
        public string? CourierPartner { get; set; }
        public string? CourierAwb { get; set; }
        public string? ResidentName { get; set; }
        public string? Unit { get; set; }
        public IReadOnlyList<PackageReturnImageDto> Photos { get; set; } = Array.Empty<PackageReturnImageDto>();
    }
}
