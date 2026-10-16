using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Domains.Abstractions.Entities;
using System.ComponentModel.DataAnnotations;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class Society : EntityBase, IAggregateRoot
    {
        [Required]
        public string Name { get; private set; } = null!;

        public Address Address { get; private set; } = null!;

        public string? ContactEmail { get; private set; }

        public string? Phone { get; private set; }

        private Society() { }
        private Society(string name, Address address, string contactEmail, string phone)
        {
            //only mandatory fields are required in constructor
            this.Name = name;
            this.Address = address;
            this.ContactEmail = contactEmail;
            this.Phone = phone;
        }
        //Factory method to restore state

        public static Society Create(string name, Address address, string contactEmail, string phone)
        {

            var society = new Society(name, address, contactEmail, phone);
            return society;
        }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));

            Name = name.Trim();
        }

        public void ChangeAddress(Address address)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
        }

        public void ChangeContact(string? contactEmail, string? phone)
        {
            ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim();
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        }
    }
}
