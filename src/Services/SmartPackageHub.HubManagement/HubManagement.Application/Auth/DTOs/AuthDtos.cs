namespace HubManagement.Application.Auth.DTOs
{
    public sealed class RequestResidentLoginOtpRequest
    {
        public string? PhoneNumber { get; set; }
    }

    public sealed class VerifyResidentLoginOtpRequest
    {
        public string? PhoneNumber { get; set; }
        public string? Otp { get; set; }
    }

    public sealed class StaffLoginRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public sealed record ResidentLoginOtpRequestResult(
        bool Succeeded,
        bool NotFound,
        Guid? ResidentId,
        string? Otp,
        string Message);

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

    public sealed record StaffLoginResult(
        bool Succeeded,
        string? Token,
        Guid? UserId,
        string? Username,
        string? Role,
        IReadOnlyList<Guid> SocietyIds,
        Guid? PrimarySocietyId,
        string Message);
}
