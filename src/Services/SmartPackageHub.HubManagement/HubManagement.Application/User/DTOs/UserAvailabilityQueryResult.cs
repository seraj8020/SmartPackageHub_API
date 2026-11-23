namespace HubManagement.Application.HubUsers.DTOs
{
    public sealed record UserAvailabilityQueryResult(bool Forbidden, bool Succeeded, string Message, bool Available);
}