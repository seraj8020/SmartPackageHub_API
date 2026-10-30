namespace HubManagement.Application.HubUsers.DTOs
{
    public sealed record UserCommandResult(bool Found, bool Conflict, bool Succeeded, string Message, UserDetailsDto? User);
}
