using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class AccessWindow : ValueObject
    {
        public DateTime ValidFrom { get; private set; }
        public DateTime ValidTo { get; private set; }

        private AccessWindow(DateTime validFrom, DateTime validTo)
        {
            ValidFrom = validFrom;
            ValidTo = validTo;
        }
        public static AccessWindow Create(DateTime validFrom, DateTime validTo)
        {
            if (validTo <= validFrom)
                throw new ArgumentException("End time must be after start time.", nameof(validTo));
            return new AccessWindow(validFrom, validTo);
        }
        public bool IsValidAt(DateTime time) => time >= ValidFrom && time <= ValidTo;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return ValidFrom;
            yield return ValidTo;
        }
    }
}
