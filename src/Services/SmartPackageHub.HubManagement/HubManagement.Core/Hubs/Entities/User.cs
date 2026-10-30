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
        /// <summary>PBKDF2 hash for staff/admin username-password login. Null for resident-linked users.</summary>
        public string? PasswordHash { get; private set; }
        /// <summary>Optional mobile number for the hub user (staff/admin/courier).</summary>
        public string? PhoneNumber { get; private set; }

        private readonly List<UserSociety> _societies = new();
        public IReadOnlyCollection<UserSociety> Societies => _societies.AsReadOnly();

        public static User Create(
            string username,
            ContactInfo contactInfo,
            Role role,
            string status = "Active",
            string? passwordHash = null,
            string? phoneNumber = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username required.", nameof(username));
            ArgumentNullException.ThrowIfNull(contactInfo);
            ArgumentNullException.ThrowIfNull(role);

            return new User(username, contactInfo, role, status, passwordHash, phoneNumber);
        }

        private User() { }

        private User(
            string username,
            ContactInfo contactInfo,
            Role role,
            string status = "Active",
            string? passwordHash = null,
            string? phoneNumber = null)
        {
            Username = username;
            ContactInfo = contactInfo;
            Role = role;
            Status = status;
            PasswordHash = passwordHash;
            PhoneNumber = NormalizePhone(phoneNumber);
        }

        public void SetPasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash is required.", nameof(passwordHash));
            PasswordHash = passwordHash;
        }

        public void SetPhoneNumber(string? phoneNumber)
        {
            PhoneNumber = NormalizePhone(phoneNumber);
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

        private static string? NormalizePhone(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;
            return phoneNumber.Trim();
        }
    }
}
