namespace HubManagement.Application.Intake.DTOs
{
    public sealed class BatchIntakeResponse
    {
        public int Total { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public IList<BatchIntakeItemResult> Results { get; } = new List<BatchIntakeItemResult>();
    }
}
