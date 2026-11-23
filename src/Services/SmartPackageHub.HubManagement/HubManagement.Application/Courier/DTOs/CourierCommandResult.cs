namespace HubManagement.Application.Couriers.DTOs
{
    public sealed class CourierCommandResult
    {
        public bool Found { get; init; } = true;
        public bool Succeeded { get; init; }
        public bool Conflict { get; init; }
        public bool Forbidden { get; init; }
        public string? Message { get; init; }
        public CourierAgentDto? Agent { get; init; }
        public CourierBlacklistDto? Blacklist { get; init; }

        public static CourierCommandResult NotFound(string message) =>
            new() { Found = false, Succeeded = false, Message = message };

        public static CourierCommandResult Fail(string message, bool conflict = false) =>
            new() { Found = true, Succeeded = false, Conflict = conflict, Message = message };

        public static CourierCommandResult FailForbidden(string message) =>
            new() { Found = true, Succeeded = false, Forbidden = true, Message = message };

        public static CourierCommandResult OkAgent(CourierAgentDto agent) =>
            new() { Found = true, Succeeded = true, Agent = agent };

        public static CourierCommandResult OkBlacklist(CourierBlacklistDto blacklist) =>
            new() { Found = true, Succeeded = true, Blacklist = blacklist };

        public static CourierCommandResult OkMessage(string message) =>
            new() { Found = true, Succeeded = true, Message = message };
    }
}