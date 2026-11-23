using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    /// <summary>
    /// One logged move in a package's internal chain of custody
    /// (hub → satellite cage → door run → delivered / failed / back).
    /// </summary>
    public sealed class PackageCustodyHop : EntityBase, IAggregateRoot
    {
        public const int ZoneMaxLength = 120;
        public const int LocationMaxLength = 80;
        public const int StaffNameMaxLength = 100;
        public const int KindMaxLength = 40;
        public const int NotesMaxLength = 500;

        public Guid PackageId { get; private set; }
        public Guid SocietyId { get; private set; }
        public string Kind { get; private set; } = null!;
        public string? FromZone { get; private set; }
        public string? ToZone { get; private set; }
        public string? LocationLabel { get; private set; }
        public Guid? StaffUserId { get; private set; }
        public string? StaffUsername { get; private set; }
        public DateTime OccurredAtUtc { get; private set; }
        public string? Notes { get; private set; }

        private PackageCustodyHop() { }

        private PackageCustodyHop(
            Guid packageId,
            Guid societyId,
            string kind,
            string? fromZone,
            string? toZone,
            string? locationLabel,
            Guid? staffUserId,
            string? staffUsername,
            DateTime occurredAtUtc,
            string? notes)
        {
            PackageId = packageId;
            SocietyId = societyId;
            Kind = kind;
            FromZone = Truncate(fromZone, ZoneMaxLength);
            ToZone = Truncate(toZone, ZoneMaxLength);
            LocationLabel = Truncate(locationLabel, LocationMaxLength);
            StaffUserId = staffUserId == Guid.Empty ? null : staffUserId;
            StaffUsername = Truncate(staffUsername, StaffNameMaxLength);
            OccurredAtUtc = occurredAtUtc;
            Notes = Truncate(notes, NotesMaxLength);
        }

        public static PackageCustodyHop Create(
            Guid packageId,
            Guid societyId,
            string kind,
            DateTime occurredAtUtc,
            string? fromZone = null,
            string? toZone = null,
            string? locationLabel = null,
            Guid? staffUserId = null,
            string? staffUsername = null,
            string? notes = null)
        {
            if (packageId == Guid.Empty)
                throw new ArgumentException("Package id is required.", nameof(packageId));
            if (societyId == Guid.Empty)
                throw new ArgumentException("Society id is required.", nameof(societyId));
            if (string.IsNullOrWhiteSpace(kind))
                throw new ArgumentException("Hop kind is required.", nameof(kind));

            return new PackageCustodyHop(
                packageId,
                societyId,
                kind.Trim(),
                fromZone,
                toZone,
                locationLabel,
                staffUserId,
                staffUsername,
                occurredAtUtc,
                notes);
        }

        private static string? Truncate(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var trimmed = value.Trim();
            return trimmed.Length <= max ? trimmed : trimmed[..max];
        }
    }
}
