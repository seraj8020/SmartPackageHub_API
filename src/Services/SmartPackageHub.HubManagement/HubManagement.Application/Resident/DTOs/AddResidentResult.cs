namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed record AddResidentResult(
        bool Succeeded,
        bool Conflict,
        Guid? Id,
        string Message);
}
