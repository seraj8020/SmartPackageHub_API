namespace HubManagement.Application.Intake.DTOs
{
    public sealed record PhotoUploadResult(bool Succeeded, string? Error, PhotoUploadResponse? Body);
}
