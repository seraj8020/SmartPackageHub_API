using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class TrackingNumber : ValueObject
    {
        public string Value { get; private set; }

        // Rules: Non-empty, alphanumeric room definitions (e.g., "Apartment 4B")
        private TrackingNumber(string value)
        {
            Value = value;
        }
        public static TrackingNumber Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
                throw new ArgumentException("Invalid tracking number format.", nameof(value));
            return new TrackingNumber(value);

        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
