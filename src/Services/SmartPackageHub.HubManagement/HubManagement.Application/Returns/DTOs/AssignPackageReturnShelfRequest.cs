namespace HubManagement.Application.Returns.DTOs
{
    public sealed class AssignPackageReturnShelfRequest
    {
        public string? ShelfCode { get; set; }
        public Guid? StaffUserId { get; set; }
    }
}
