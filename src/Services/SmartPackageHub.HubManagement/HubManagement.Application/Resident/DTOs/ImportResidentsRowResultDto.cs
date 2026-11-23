namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed class ImportResidentsRowResultDto
    {
        public int Line { get; init; }
        public string? Phone { get; init; }
        public string Status { get; init; } = "failed";
        public string Message { get; init; } = string.Empty;
    }
}
