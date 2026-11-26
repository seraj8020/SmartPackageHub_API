namespace HubManagement.Application.Support.DTOs
{
    public sealed class SupportCommandResult
    {
        public bool Succeeded { get; init; }
        public bool Found { get; init; } = true;
        public string Message { get; init; } = string.Empty;
        public SupportRequestDto? Request { get; init; }

        public static SupportCommandResult Ok(SupportRequestDto request, string message) =>
            new() { Succeeded = true, Message = message, Request = request };

        public static SupportCommandResult Fail(string message, bool found = true) =>
            new() { Succeeded = false, Found = found, Message = message };

        public static SupportCommandResult NotFound(string message) =>
            Fail(message, found: false);
    }
}
