namespace HubManagement.Application.Intake.DTOs
{
    public sealed record IntakeStatusResult<T>(int StatusCode, string? Error, T? Body);
}
