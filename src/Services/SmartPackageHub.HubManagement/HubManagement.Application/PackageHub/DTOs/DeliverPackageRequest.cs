namespace HubManagement.Application.PackageHub.DTOs
{
    /// <summary>
    /// Minimal deliver body. Society / resident / staff are taken from the package when omitted.
    /// </summary>
    public class DeliverPackageRequest
    {
        public string? Token { get; set; }
        public string? Method { get; set; }
        public string? Notes { get; set; }
        public Guid? BatchId { get; set; }

        // Optional overrides (legacy clients). Prefer package values when empty.
        public Guid? SocietyId { get; set; }
        public Guid? StaffUserId { get; set; }
        public Guid? ResidentId { get; set; }
    }
}
