namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed record GetResidentVacationHoldResult(bool ResidentFound, VacationHoldDto? Hold);
}
