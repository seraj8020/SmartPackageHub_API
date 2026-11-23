using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    /// <summary>
    /// Outcome of a door/counter delivery attempt when not a successful handover.
    /// Stored on <c>Deliveries.FailureReason</c> for audit / re-shelve flows.
    /// </summary>
    public sealed class DeliveryAttemptOutcome : ValueObject
    {
        public string Value { get; }

        private DeliveryAttemptOutcome(string value) => Value = value;

        public static DeliveryAttemptOutcome ResidentUnavailable => new("ResidentUnavailable");
        public static DeliveryAttemptOutcome Refused => new("Refused");
        public static DeliveryAttemptOutcome Other => new("Other");

        public static DeliveryAttemptOutcome From(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalized))
                throw new ArgumentException("Delivery attempt outcome is required.", nameof(value));

            if (string.Equals(normalized, ResidentUnavailable.Value, StringComparison.OrdinalIgnoreCase))
                return ResidentUnavailable;
            if (string.Equals(normalized, Refused.Value, StringComparison.OrdinalIgnoreCase))
                return Refused;
            if (string.Equals(normalized, Other.Value, StringComparison.OrdinalIgnoreCase))
                return Other;

            throw new ArgumentException(
                $"Unsupported delivery attempt outcome '{normalized}'.",
                nameof(value));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;
    }
}
