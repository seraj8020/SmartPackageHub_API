namespace HubManagement.Application.Intake.DTOs
{
    public sealed class MatchResponse
    {
        public string Status { get; set; } = "missing";
#pragma warning disable CA2227 // Collection properties should be read only
        public required IList<ResidentMatchDto> Matches { get; set; } = new List<ResidentMatchDto>();
#pragma warning restore CA2227
    }
}
