namespace HubManagement.Application.Auth.DTOs
{
    public sealed class VerifyPlatformAdminMfaRequest
    {
        public string? MfaToken { get; set; }
        public string? Code { get; set; }
    }
}
