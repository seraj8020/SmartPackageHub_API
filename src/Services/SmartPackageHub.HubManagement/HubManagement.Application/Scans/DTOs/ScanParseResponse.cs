namespace HubManagement.Application.Scans.DTOs
{
    public sealed class ScanParseResponse
    {
        public string Vendor { get; set; } = string.Empty;
        public string Raw { get; set; } = string.Empty;
        public string Tracking { get; set; } = string.Empty;
    }
}
