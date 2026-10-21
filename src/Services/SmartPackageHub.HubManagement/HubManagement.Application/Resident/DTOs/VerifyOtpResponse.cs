namespace HubManagement.Application.HubResidents.DTOs
{
    public sealed record VerifyOtpResponse(bool ResidentFound, bool Succeeded, string Message);
}
