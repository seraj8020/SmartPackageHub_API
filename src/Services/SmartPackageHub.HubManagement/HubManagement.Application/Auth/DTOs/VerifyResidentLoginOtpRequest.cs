
namespace HubManagement.Application.Auth.DTOs
{
    public sealed class VerifyResidentLoginOtpRequest
    {
        public string? PhoneNumber { get; set; }
        public string? Otp { get; set; }
    }
}
