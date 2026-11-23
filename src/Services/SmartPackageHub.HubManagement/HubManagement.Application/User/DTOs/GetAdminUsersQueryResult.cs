namespace HubManagement.Application.HubUsers.DTOs
{
    public sealed record GetAdminUsersQueryResult(bool Forbidden, bool Succeeded, string Message, IReadOnlyList<UserDetailsDto> Users);
}