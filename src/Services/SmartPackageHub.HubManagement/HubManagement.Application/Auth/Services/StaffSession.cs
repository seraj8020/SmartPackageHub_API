namespace HubManagement.Application.Auth.Services
{
    public sealed record StaffSession(Guid UserId, string Role, IReadOnlyList<Guid> SocietyIds);
}
