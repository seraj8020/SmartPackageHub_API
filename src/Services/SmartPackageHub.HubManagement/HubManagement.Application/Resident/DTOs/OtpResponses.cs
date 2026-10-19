namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed record RequestOtpResponse(bool ResidentFound, Guid? ResidentId, string? Otp, string Message);

    public sealed record VerifyOtpResponse(bool ResidentFound, bool Succeeded, string Message);

    
}
