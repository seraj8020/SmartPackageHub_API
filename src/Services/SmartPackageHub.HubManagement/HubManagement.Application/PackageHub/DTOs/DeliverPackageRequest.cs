namespace HubManagement.Application.PackageHub.DTOs
{
    /// <summary>
    /// Deliver body. OTP is required; photo / signature / notes are optional proof-of-delivery extras.
    /// Society / resident / staff are taken from the package when omitted.
    /// </summary>
    public class DeliverPackageRequest
    {
        public string? Token { get; set; }
        public string? Method { get; set; }
        public string? Notes { get; set; }
        public Guid? BatchId { get; set; }

        /// <summary>Optional base64 (or data-URL) handover photo.</summary>
        public string? PhotoBase64 { get; set; }

        /// <summary>Optional base64 (or data-URL) resident signature image (PNG preferred).</summary>
        public string? SignatureBase64 { get; set; }

        /// <summary>Name shown under the signature pad.</summary>
        public string? SignedByName { get; set; }

        // Optional overrides (legacy clients). Prefer package values when empty.
        public Guid? SocietyId { get; set; }
        public Guid? StaffUserId { get; set; }
        public Guid? ResidentId { get; set; }
    }
}
