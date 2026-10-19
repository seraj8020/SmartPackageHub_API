using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class Membership: EntityBase, IAggregateRoot
    {
        public Guid ResidentId { get; private set; }
        public Guid SocietyId { get; private set; }
        public SubscriptionTier Tier { get; private set; } = null!;
        public ValidityPeriod Period { get; private set; } = null!;
        public string Status { get; private set; } = null!;
        public bool IsAutoRenewEnabled { get; private set; }

        private Membership() { }
        // Constructor for creating a new membership
        private Membership(Guid residentId, Guid societyId, SubscriptionTier tier, ValidityPeriod period, string status, bool isAutoRenewEnabled)
        {
            ResidentId = residentId;
            SocietyId = societyId;
            Tier = tier;
            Period = period;
            Status = status;
            IsAutoRenewEnabled = isAutoRenewEnabled;
        }
        public static Membership Create(Guid residentId, Guid societyId, SubscriptionTier tier, ValidityPeriod period, string status, bool isAutoRenewEnabled)
        {
            if(tier == null)
                throw new ArgumentNullException(nameof(tier));
            if(period == null)
                throw new ArgumentNullException(nameof(period));
            var memberShip = new Membership(residentId, societyId, tier, period, status, isAutoRenewEnabled);
            return memberShip;
        }
        // Domain Behavior: Upgrade Tier
        public void UpgradeTier(SubscriptionTier newTier, ValidityPeriod newPeriod)
        {
            if (newTier == null) throw new ArgumentNullException(nameof(newTier));
            if (newPeriod == null) throw new ArgumentNullException(nameof(newPeriod));

            // Business logic/rule validation can go here
            Tier = newTier;
            Period = newPeriod;
        }

        // Domain Behavior: Toggle Auto-Renew
        public void ToggleAutoRenew(bool enabled)
        {
            IsAutoRenewEnabled = enabled;
        }

        // Business Rule check
        public bool IsMembershipValid(DateTime currentTime)
        {
            return Period.IsActive(currentTime);
        }
    }
}
