namespace HubManagement.Application.Intake.DTOs
{
    public sealed class ParsedLabelDto
    {
        public string? Courier { get; set; }
        public string? Awb { get; set; }
        public string? RecipientName { get; set; }
        public string? Tower { get; set; }
        public string? FlatNumber { get; set; }
    }
}
