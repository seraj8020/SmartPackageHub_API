namespace HubManagement.Application.Intake.DTOs
{
    public sealed class BatchIntakeItemResult
    {
        public string? ClientRequestId { get; set; }
        public string? Awb { get; set; }
        public bool Succeeded { get; set; }
        public int StatusCode { get; set; }
        public string? Error { get; set; }
        public ConfirmIntakeResponse? Intake { get; set; }
    }
}
