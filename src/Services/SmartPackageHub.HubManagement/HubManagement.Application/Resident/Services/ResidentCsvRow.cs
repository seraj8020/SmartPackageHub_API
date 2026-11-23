namespace HubManagement.Application.HubResidents.Services
{
    internal sealed record ResidentCsvRow(
        int Line,
        string FullName,
        string Phone,
        string TowerWing,
        string FlatNumber,
        bool IsMember);
}
