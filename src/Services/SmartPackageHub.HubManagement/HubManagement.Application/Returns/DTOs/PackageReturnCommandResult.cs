namespace HubManagement.Application.Returns.DTOs
{
    public sealed class PackageReturnCommandResult
    {
        public bool Found { get; init; } = true;
        public bool Succeeded { get; init; }
        public bool Conflict { get; init; }
        public bool Forbidden { get; init; }
        public string? Message { get; init; }
        public PackageReturnDto? Return { get; init; }

        public static PackageReturnCommandResult NotFound(string message) =>
            new() { Found = false, Succeeded = false, Message = message };

        public static PackageReturnCommandResult Fail(string message, bool conflict = false) =>
            new() { Found = true, Succeeded = false, Conflict = conflict, Message = message };

        public static PackageReturnCommandResult FailForbidden(string message) =>
            new() { Found = true, Succeeded = false, Forbidden = true, Message = message };

        public static PackageReturnCommandResult Ok(PackageReturnDto dto, string? message = null) =>
            new() { Found = true, Succeeded = true, Return = dto, Message = message };
    }
}
