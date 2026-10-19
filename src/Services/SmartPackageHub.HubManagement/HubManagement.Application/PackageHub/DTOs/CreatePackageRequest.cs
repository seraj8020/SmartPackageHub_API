
namespace HubManagement.Application.PackageHub.DTOs
{
    public sealed class CreatePackageRequest
    {
        public string? TrackingNumber { get; set; }
        public string? Description { get; set; }
        public Guid? ResidentId { get; set; }
        public Guid? SocietyId { get; set; }
        public Guid? StaffUserId { get; set; }
        public string? Courier { get; set; }
        public string? Tower { get; set; }
        public string? FlatNumber { get; set; }
        public string? RecipientName { get; set; }
    }
}
