using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class ExpiryTime : ValueObject
    {
        public DateTime Value { get; private set; }
        public bool IsExpired => DateTime.UtcNow > Value;
        
        private ExpiryTime(DateTime value)
        {
            Value = value;
        }
        public static ExpiryTime Create(DateTime value)
        {
            if (value <= DateTime.UtcNow)
                throw new ArgumentException("Expiry time must be in the future.", nameof(value));
            return new ExpiryTime(value);
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
