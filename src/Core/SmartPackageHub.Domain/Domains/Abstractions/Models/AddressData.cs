
namespace SmartPackageHub.Domains.Abstractions.Models
{
    public record class AddressData
    {
        public string street { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string state { get; set; } = string.Empty;
        public string zipCode { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;

        public AddressData() { }

        public AddressData(string street, string city, string state, string zipCode, string country)
        {
            this.street = street;
            this.city = city;
            this.state = state;
            this.zipCode = zipCode;
            this.country = country;
        }

        public void Deconstruct(out string street, out string city, out string state, out string zipCode, out string country)
        {
            street = this.street;
            city = this.city;
            state = this.state;
            zipCode = this.zipCode;
            country = this.country;
        }
    }
}
