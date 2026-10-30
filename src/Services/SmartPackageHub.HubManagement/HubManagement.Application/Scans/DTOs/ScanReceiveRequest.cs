namespace HubManagement.Application.Scans.DTOs
{
    public sealed class ScanReceiveRequest
    {
        public string? Vendor { get; set; }
        public string? RawCode { get; set; }
        public string? Description { get; set; }
        public Guid? ResidentId { get; set; }
        public Guid? SocietyId { get; set; }
        public Guid? StaffUserId { get; set; }
        public string? Tower { get; set; }
        public string? FlatNumber { get; set; }
        public string? RecipientName { get; set; }
    }

    

    
}
