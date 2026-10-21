using SmartPackageHub.Domains.Abstractions.ValueObjects;
using System.Text.RegularExpressions;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class UnitNumber : ValueObject
    {
        private static readonly Regex UnitPattern = new(@"^[a-zA-Z0-9\s-]+$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(250));

        public string Value { get;  private set; }

        // Rules: Non-empty, alphanumeric room definitions (e.g., "Apartment 4B")
        private UnitNumber(string value)
        {
            Value = value;
        }
        public static UnitNumber Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Unit number cannot be empty.", nameof(value));

            var trimmedValue = value.Trim();
            if (!UnitPattern.IsMatch(trimmedValue))
                throw new ArgumentException("Unit number contains invalid characters.", nameof(value));
            return new UnitNumber(trimmedValue);
            
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value.ToUpperInvariant(); // Case-insensitive structural equality
        }

        public override string ToString() => Value;

    }
}
