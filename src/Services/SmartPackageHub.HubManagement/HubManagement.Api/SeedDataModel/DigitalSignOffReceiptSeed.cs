namespace HubManagement.Api.SeedDataModel
{
    internal sealed class DigitalSignOffReceiptSeed
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public string CourierPartner { get; set; } = string.Empty;
        public Guid? CourierAgentId { get; set; }
        public string CourierAgentName { get; set; } = string.Empty;
        public string CourierAgentPhone { get; set; } = string.Empty;
        public Guid StaffUserId { get; set; }
        public int TotalPackagesCount { get; set; }
        public string? DigitalSignature { get; set; }
        public string Status { get; set; } = "Acknowledged";
        public DateTime SignedAt { get; set; }
        public string? Notes { get; set; }
    }
}
