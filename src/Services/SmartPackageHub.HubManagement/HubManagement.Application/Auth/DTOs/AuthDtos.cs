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

    public sealed class PlatformAdminLoginRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public sealed class VerifyPlatformAdminMfaRequest
    {
        public string? MfaToken { get; set; }
        public string? Code { get; set; }
    }

    public sealed class ChangePlatformAdminPasswordRequest
    {
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }

    public sealed class ForgotPlatformAdminPasswordRequest
    {
        public string? UsernameOrEmail { get; set; }
    }

    public sealed class ForgotPlatformAdminUsernameRequest
    {
        public string? Email { get; set; }
    }

    public sealed class ResetPlatformAdminPasswordRequest
    {
        public string? ResetToken { get; set; }
        public string? Code { get; set; }
        public string? NewPassword { get; set; }
    }

    public sealed record PlatformAdminLoginResult(
        bool Succeeded,
        bool MfaRequired,
        string? MfaToken,
        string? EmailHint,
        string? DevelopmentCode,
        string? AccessToken,
        string Message,
        string? Role = null,
        string? Username = null,
        IReadOnlyList<Guid>? SocietyIds = null);

    public sealed record PlatformAdminPasswordChangeResult(
        bool Succeeded,
        bool Unauthorized,
        string Message);

    public sealed record PlatformAdminForgotPasswordResult(
        bool Succeeded,
        string? ResetToken,
        string? EmailHint,
        string? DevelopmentCode,
        string Message);

    public sealed record PlatformAdminResetPasswordResult(
        bool Succeeded,
        string Message);

    public sealed record PlatformAdminForgotUsernameResult(
        bool Succeeded,
        string? Username,
        string? EmailHint,
        string Message);
}
