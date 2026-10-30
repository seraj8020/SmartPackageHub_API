
namespace HubManagement.Application.Auth.DTOs
{
    public sealed record ResidentLoginOtpRequestResult(
        bool Succeeded,
        bool NotFound,
        Guid? ResidentId,
        string? Otp,
        string Message);
}
