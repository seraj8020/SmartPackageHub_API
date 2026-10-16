using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class DeliveryStatus : ValueObject
    {
        public string Value { get;}

        private DeliveryStatus(string value) => Value = value;

        public static DeliveryStatus Attempted => new DeliveryStatus("Attempted");
        public static DeliveryStatus Completed => new DeliveryStatus("Completed");
        public static DeliveryStatus Rejected => new DeliveryStatus("Rejected");

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
