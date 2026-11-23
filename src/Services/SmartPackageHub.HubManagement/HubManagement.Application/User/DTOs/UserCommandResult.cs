namespace HubManagement.Application.HubUsers.DTOs
{
    public sealed record UserCommandResult(
        bool Found,
        bool Conflict,
        bool Succeeded,
        bool Forbidden,
        string Message,
        UserDetailsDto? User,
        string? ConflictField = null);
}
