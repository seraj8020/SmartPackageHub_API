using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Domains.Abstractions.Entities;
using System.ComponentModel.DataAnnotations;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class User : EntityBase, IAggregateRoot
    {
        [Required]
        public string Username { get; private set; } = null!;
        public ContactInfo? ContactInfo { get; private set; }
        public Role Role { get; private set; } = null!;
        public string Status { get; private set; } = "Active";

        private readonly List<UserSociety> _societies = new();
        public IReadOnlyCollection<UserSociety> Societies => _societies.AsReadOnly();
        private User() { }
        public static User Create(string username, ContactInfo contactInfo, Role role, string status = "Active")
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username required.");
            if (contactInfo == null)
                throw new ArgumentNullException(nameof(contactInfo));
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            return new User(username, contactInfo, role, status);
        }
        private User(string username, ContactInfo contactInfo, Role role, string status = "Active")
        {
            Username = username;
            ContactInfo = contactInfo;
            Role = role;
            Status = status;
        }

        public void LinkToSociety(Guid societyId)
        {
            if (_societies.Exists(s => s.SocietyId == societyId))
                throw new InvalidOperationException("User is already linked to this society.");
            _societies.Add(UserSociety.Create(Id, societyId));
        }

        public void ChangeContact(ContactInfo contactInfo)
        {
            ContactInfo = contactInfo ?? throw new ArgumentNullException(nameof(contactInfo));
        }

        public void ChangeRole(Role role)
        {
            Role = role ?? throw new ArgumentNullException(nameof(role));
        }

        public void ChangeStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Status is required.", nameof(status));

            Status = status.Trim();
        }
    }
}
