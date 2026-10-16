using SmartPackageHub.Domains.Abstractions.Guards;
using SmartPackageHub.Domains.Abstractions.Models;
using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class Address : ValueObject
    {
        public string Street { get; private set; }
        public string City { get; private set; }
        public string State { get; private set; }
        public string ZipCode { get; private set; }
        public string Country { get; private set; }
        public static Address Create(AddressData addressData)
        {
            var (street, city, state, zipCode, country) = addressData ?? throw new ArgumentNullException(nameof(addressData));
            if (string.IsNullOrWhiteSpace(street))
                throw new BusinessRuleException("street cannot be null or whitespace.");
            if (string.IsNullOrWhiteSpace(city))
                throw new BusinessRuleException("city cannot be null or whitespace.");
            if (string.IsNullOrWhiteSpace(state))
                throw new BusinessRuleException("state cannot be null or whitespace.");
            if (string.IsNullOrWhiteSpace(zipCode))
                throw new BusinessRuleException("zipCode cannot be null or whitespace.");
            if (string.IsNullOrWhiteSpace(country))
                throw new BusinessRuleException("country cannot be null or whitespace.");

            return new Address(street, city, state, zipCode, country);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return State;
            yield return ZipCode;
            yield return Country;
        }

        private Address(string street, string city, string state, string zipCode, string country)
        {
            this.Street = street;
            this.City = city;
            this.State = state;
            this.ZipCode = zipCode;
            this.Country = country;
        }
    }
}
