namespace HubManagement.Application.Returns.DTOs
{
    public sealed class RequestPackageReturnRequest
    {
        public Guid ResidentId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
