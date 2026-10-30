using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class ContactInfo : ValueObject
    {
        public string Email { get; private set; }

        private ContactInfo(string email)
        {
            Email = email;
        }
        public static ContactInfo Create(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
            return new ContactInfo(email);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Email;
        }
    }
}
