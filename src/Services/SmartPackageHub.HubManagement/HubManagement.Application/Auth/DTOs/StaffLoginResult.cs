
namespace HubManagement.Application.Auth.DTOs
{
    public sealed record StaffLoginResult(
        bool Succeeded,
        string? Token,
        Guid? UserId,
        string? Username,
        string? Role,
        IReadOnlyList<Guid> SocietyIds,
        Guid? PrimarySocietyId,
        string Message);
}
