using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    /// <summary>
    /// Resident-authorized person who may collect a specific hub package on their behalf.
    /// </summary>
    public sealed class AuthorizedCollector : EntityBase, IAggregateRoot
    {
        public Guid PackageId { get; private set; }
        public Guid SocietyId { get; private set; }
        public Guid ResidentId { get; private set; }
        public string CollectorName { get; private set; } = null!;
        public string? CollectorPhone { get; private set; }
        public string? Note { get; private set; }
        public DateTime AuthorizedAtUtc { get; private set; }
        public DateTime? ExpiresAtUtc { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime? RevokedAtUtc { get; private set; }

        private AuthorizedCollector() { }

        private AuthorizedCollector(
            Guid packageId,
            Guid societyId,
            Guid residentId,
            string collectorName,
            string? collectorPhone,
            string? note,
            DateTime authorizedAtUtc,
            DateTime? expiresAtUtc)
        {
            PackageId = packageId;
            SocietyId = societyId;
            ResidentId = residentId;
            CollectorName = collectorName;
            CollectorPhone = collectorPhone;
            Note = note;
            AuthorizedAtUtc = authorizedAtUtc;
            ExpiresAtUtc = expiresAtUtc;
            IsActive = true;
        }

        public static AuthorizedCollector Create(
            Guid packageId,
            Guid societyId,
            Guid residentId,
            string collectorName,
            string? collectorPhone,
            string? note,
            DateTime? expiresAtUtc = null)
        {
            var name = (collectorName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Collector name is required.", nameof(collectorName));
            if (name.Length > 150)
                throw new ArgumentException("Collector name is too long.", nameof(collectorName));

            var phone = string.IsNullOrWhiteSpace(collectorPhone) ? null : collectorPhone.Trim();
            if (phone is { Length: > 30 })
                throw new ArgumentException("Collector phone is too long.", nameof(collectorPhone));

            var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            if (trimmedNote is { Length: > 300 })
                throw new ArgumentException("Note is too long.", nameof(note));

            var now = DateTime.UtcNow;
            if (expiresAtUtc is DateTime expires && expires <= now)
                throw new ArgumentException("Expiry must be in the future.", nameof(expiresAtUtc));

            return new AuthorizedCollector(
                packageId,
                societyId,
                residentId,
                name,
                phone,
                trimmedNote,
                now,
                expiresAtUtc);
        }

        public bool IsValidAt(DateTime utcNow) =>
            IsActive && (ExpiresAtUtc is null || ExpiresAtUtc > utcNow);

        public void Revoke()
        {
            if (!IsActive)
                return;

            IsActive = false;
            RevokedAtUtc = DateTime.UtcNow;
        }
    }
}
