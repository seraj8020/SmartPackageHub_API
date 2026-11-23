namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record TopScannerDto(
        Guid StaffUserId,
        string DisplayName,
        int ScanCount);
}
