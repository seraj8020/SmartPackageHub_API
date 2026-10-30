namespace HubManagement.Api.SeedDataModel
{
    internal sealed class AnalyticsDailySeed
    {
        public Guid Id { get; set; }
        public Guid SocietyId { get; set; }
        public DateTime Day { get; set; }
        public int PackagesReceived { get; set; }
        public int PackagesDelivered { get; set; }
        public int OverdueCount { get; set; }
    }
}
