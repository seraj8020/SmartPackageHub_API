namespace HubManagement.Application.Societies.DTOs
{
    public sealed record SocietyCommandResult(bool Found, bool Succeeded, string Message, SocietyDetailsDto? Society);
}
