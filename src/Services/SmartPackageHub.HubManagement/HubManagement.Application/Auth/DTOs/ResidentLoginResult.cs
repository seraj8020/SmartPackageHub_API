namespace HubManagement.Application.Auth.DTOs
{
    public sealed record ResidentLoginResult(
        bool Succeeded,
        bool NotFound,
        string? Token,
        Guid? ResidentId,
        Guid? SocietyId,
        string? FullName,
        string? PhoneNumber,
        string Role,
        string Message);
}
