namespace HubManagement.Application.Returns.DTOs
{
    public sealed class MarkPackageReturnCourierPickupRequest
    {
        public string? Note { get; set; }
        public string? Awb { get; set; }
        public Guid? StaffUserId { get; set; }
    }
}
