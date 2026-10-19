using SmartPackageHub.Domains.Abstractions.Entities;
using System.ComponentModel.DataAnnotations;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class PackageImage : EntityBase
    {
        public Guid PackageId { get; private set; }
        public Guid IntakeId { get; private set; }
        public string Kind { get; private set; } = null!;// e.g. "front", "back", "side", "label", "Damage", "Evidence"

        [Required]
        public string StorageKey { get; private set; } = ""; // Storage key for the image in blob storage start with SocietyId

        [Required]
        public string SHA256Hash { get; private set; } = ""; // SHA 256 hash of the image for integrity check

        [Required]
        public string ContentType { get; set; } = "image/jpeg";

        public long ByteSize { get; set; }
        public DateTime CapturedAt { get; private set; } = DateTime.UtcNow;
        public PackageIntake? Intake { get; private set; }

        private PackageImage() { }
        private PackageImage(Guid packageId, Guid intakeId, string kind, string storageKey, string sha256Hash, DateTime capturedAt)
        {
            this.PackageId = packageId;
            this.IntakeId = intakeId;
            this.Kind = kind;
            this.StorageKey = storageKey;
            this.SHA256Hash = sha256Hash;
            this.CapturedAt = capturedAt;
        }

        public static PackageImage Create(Guid packageId, Guid intakeId, string kind, string storageKey, string sha256Hash, DateTime capturedAt)
        {
            var packageImage = new PackageImage(packageId, intakeId, kind, storageKey, sha256Hash, capturedAt);
            return packageImage;
        }
    }
}
