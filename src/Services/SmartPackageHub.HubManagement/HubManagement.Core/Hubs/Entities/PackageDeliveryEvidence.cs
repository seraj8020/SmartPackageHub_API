using SmartPackageHub.Domains.Abstractions.Entities;
using System.ComponentModel.DataAnnotations;

namespace HubManagement.Core.Hubs.Entities
{
    /// <summary>
    /// Timestamped proof-of-delivery artifact (handover photo or resident signature) linked to a <see cref="Delivery"/>.
    /// Separate from intake damage / return photos.
    /// </summary>
    public sealed class PackageDeliveryEvidence : EntityBase
    {
        public Guid DeliveryId { get; private set; }
        public Guid PackageId { get; private set; }

        /// <summary><see cref="Models.DeliveryEvidenceKind"/> — Photo or Signature.</summary>
        [Required]
        [MaxLength(40)]
        public string Kind { get; private set; } = "";

        [Required]
        [MaxLength(500)]
        public string StorageKey { get; private set; } = "";

        [Required]
        [MaxLength(128)]
        public string SHA256Hash { get; private set; } = "";

        [Required]
        [MaxLength(100)]
        public string ContentType { get; private set; } = "image/jpeg";

        public long ByteSize { get; private set; }

        public DateTime CapturedAtUtc { get; private set; }

        /// <summary>Optional name under the signature pad (Traizr-style "Signed by").</summary>
        [MaxLength(120)]
        public string? SignedByName { get; private set; }

        private PackageDeliveryEvidence() { }

        private PackageDeliveryEvidence(
            Guid deliveryId,
            Guid packageId,
            string kind,
            string storageKey,
            string sha256Hash,
            string contentType,
            long byteSize,
            DateTime capturedAtUtc,
            string? signedByName)
        {
            DeliveryId = deliveryId;
            PackageId = packageId;
            Kind = kind;
            StorageKey = storageKey;
            SHA256Hash = sha256Hash;
            ContentType = contentType;
            ByteSize = byteSize;
            CapturedAtUtc = capturedAtUtc;
            SignedByName = signedByName;
        }

        public static PackageDeliveryEvidence Create(
            Guid deliveryId,
            Guid packageId,
            string kind,
            string storageKey,
            string sha256Hash,
            string contentType,
            long byteSize,
            DateTime capturedAtUtc,
            string? signedByName = null)
        {
            if (deliveryId == Guid.Empty)
                throw new ArgumentException("Delivery id is required.", nameof(deliveryId));
            if (packageId == Guid.Empty)
                throw new ArgumentException("Package id is required.", nameof(packageId));
            if (string.IsNullOrWhiteSpace(kind))
                throw new ArgumentException("Evidence kind is required.", nameof(kind));
            if (string.IsNullOrWhiteSpace(storageKey))
                throw new ArgumentException("Storage key is required.", nameof(storageKey));
            if (string.IsNullOrWhiteSpace(sha256Hash))
                throw new ArgumentException("SHA-256 hash is required.", nameof(sha256Hash));
            if (byteSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(byteSize), "Byte size must be positive.");

            return new PackageDeliveryEvidence(
                deliveryId,
                packageId,
                kind.Trim(),
                storageKey.Trim(),
                sha256Hash.Trim(),
                string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType.Trim(),
                byteSize,
                capturedAtUtc,
                string.IsNullOrWhiteSpace(signedByName) ? null : signedByName.Trim());
        }
    }
}
