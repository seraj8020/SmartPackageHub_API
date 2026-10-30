using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class SubscriptionTier : ValueObject
    {
        public static SubscriptionTier Free => new("Free");
        public static SubscriptionTier Silver => new("Silver");
        public static SubscriptionTier Gold => new("Gold");

        public string Name { get; private set; }

        private SubscriptionTier(string name)
        {
            Name = name;
        }

        public static SubscriptionTier Create(string tierName)
        {
            if (string.IsNullOrWhiteSpace(tierName))
                throw new ArgumentException("Tier name cannot be empty.", nameof(tierName));

            return tierName.Trim().ToUpperInvariant() switch
            {
                "FREE" => Free,
                "SILVER" => Silver,
                "GOLD" => Gold,
                _ => throw new ArgumentException($"Unsupported subscription tier: {tierName}", nameof(tierName))
            };
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
        }
    }
}
