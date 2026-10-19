
using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class OtpToken : ValueObject
    {
        public string Value { get; private set;}
        private OtpToken(string value)
        {
            Value = value;
        }
        public static OtpToken Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 4)
                throw new ArgumentException("OTP token must be a secure, non-empty string.", nameof(value));
            return new OtpToken(value);
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
