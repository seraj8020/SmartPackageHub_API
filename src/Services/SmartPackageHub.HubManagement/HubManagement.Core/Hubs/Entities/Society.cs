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

        /// <summary>Hours after shelf assignment before a waiting package is overdue (default 24).</summary>
        public int OverdueAfterHours { get; private set; } = 24;

        /// <summary>Local time of day for daily collection reminders (society timezone).</summary>
        public TimeOnly CollectionReminderLocalTime { get; private set; } = new(10, 0);

        /// <summary>IANA or Windows timezone id for reminder scheduling (e.g. Asia/Kolkata).</summary>
        public string TimeZoneId { get; private set; } = "Asia/Kolkata";

        private Society() { }
        private Society(string name, Address address, string contactEmail, string phone)
        {
            //only mandatory fields are required in constructor
            this.Name = name;
            this.Address = address;
            this.ContactEmail = contactEmail;
            this.Phone = phone;
            OverdueAfterHours = 24;
            CollectionReminderLocalTime = new TimeOnly(10, 0);
            TimeZoneId = "Asia/Kolkata";
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

        /// <summary>Hub overdue + daily collection reminder policies (society-scoped).</summary>
        public void ConfigureOverdueAndReminderPolicies(
            int overdueAfterHours,
            TimeOnly collectionReminderLocalTime,
            string timeZoneId)
        {
            if (overdueAfterHours < 1 || overdueAfterHours > 720)
                throw new ArgumentOutOfRangeException(nameof(overdueAfterHours), "Overdue threshold must be 1–720 hours.");
            if (string.IsNullOrWhiteSpace(timeZoneId))
                throw new ArgumentException("Time zone id is required.", nameof(timeZoneId));

            OverdueAfterHours = overdueAfterHours;
            CollectionReminderLocalTime = collectionReminderLocalTime;
            TimeZoneId = timeZoneId.Trim();
        }
    }
}

