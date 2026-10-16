using SmartPackageHub.Domains.Abstractions.Guards;
using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class ShelfLocation : ValueObject
    {
        public string Zone { get; private set; }
        public string Aisle { get; private set; }
        public int Tier { get; private set; }

        public static ShelfLocation Create(string zone, string aisle, int tier)
        {
            if (string.IsNullOrWhiteSpace(zone))
                throw new BusinessRuleException("zone cannot be null or whitespace.");
            if (string.IsNullOrWhiteSpace(aisle))
                throw new BusinessRuleException("aisle cannot be null or whitespace.");
            if (tier ==0)
                throw new BusinessRuleException("tier cannot be null or whitespace.");
            return new ShelfLocation(zone, aisle, tier);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Zone;
            yield return Aisle;
            yield return Tier;
        }

        private ShelfLocation(string zone, string aisle, int tier)
        {
            this.Zone = zone;
            this.Aisle = aisle;
            this.Tier = tier;
        }
    }
}
