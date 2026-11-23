namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed class ImportResidentsResultDto
    {
        public int Created { get; init; }
        public int Skipped { get; init; }
        public int Failed { get; init; }
        public IReadOnlyList<ImportResidentsRowResultDto> Rows { get; init; } = Array.Empty<ImportResidentsRowResultDto>();
        public bool Forbidden { get; init; }
        public string? Message { get; init; }
    }
}
