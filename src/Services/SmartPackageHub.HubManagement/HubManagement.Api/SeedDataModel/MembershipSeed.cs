namespace HubManagement.Api.SeedDataModel
{
    internal sealed class MembershipSeed
    {
        public Guid Id { get; set; }
        public Guid ResidentId { get; set; }
        public Guid SocietyId { get; set; }
        public string Tier { get; set; } = "Silver";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsAutoRenewEnabled { get; set; }
    }

}
