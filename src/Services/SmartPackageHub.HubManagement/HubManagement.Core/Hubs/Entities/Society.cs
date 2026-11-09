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
        public DateTime? OnboardedAtUtc { get; private set; }

        private Society() { }
        private Society(string name, Address address, string contactEmail, string phone)
        {
            //only mandatory fields are required in constructor
            this.Name = name;
            this.Address = address;
            this.ContactEmail = contactEmail;
            this.Phone = phone;
            MarkOnboarded(DateTime.UtcNow);
        }
        //Factory method to restore state

        public static Society Create(string name, Address address, string contactEmail, string phone)
        {
            return new Society(name, address, contactEmail, phone);
        }

        /// <summary>
        /// Ensures an onboard timestamp is present (create path + backfill for legacy null rows).
        /// </summary>
        public void MarkOnboarded(DateTime? atUtc = null)
        {
            if (OnboardedAtUtc.HasValue)
                return;

            var value = atUtc ?? DateTime.UtcNow;
            OnboardedAtUtc = value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);
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

