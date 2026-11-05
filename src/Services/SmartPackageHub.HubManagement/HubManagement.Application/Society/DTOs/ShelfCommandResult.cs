namespace HubManagement.Application.Societies.DTOs
{
    public sealed record ShelfCommandResult(bool Found, bool Succeeded, string Message, SocietyShelfDto? Shelf);
}
