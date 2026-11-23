using SmartPackageHub.Domains.Abstractions.Entities;
using System.ComponentModel.DataAnnotations;

namespace HubManagement.Core.Hubs.Entities
{
    /// <summary>Resident-uploaded photo evidence for a return request (not intake damage).</summary>
    public sealed class PackageReturnImage : EntityBase
    {
        public Guid ReturnId { get; private set; }

        [Required]
        public string StorageKey { get; private set; } = "";

        [Required]
        public string SHA256Hash { get; private set; } = "";

        [Required]
        public string ContentType { get; private set; } = "image/jpeg";

        public long ByteSize { get; private set; }

        /// <summary>0–2 display order (max 3 photos per return).</summary>
        public int SortOrder { get; private set; }

        public DateTime CapturedAtUtc { get; private set; }

        public PackageReturn? Return { get; }

        private PackageReturnImage() { }

        private PackageReturnImage(
            Guid returnId,
            string storageKey,
            string sha256Hash,
            string contentType,
            long byteSize,
            int sortOrder,
            DateTime capturedAtUtc)
        {
            ReturnId = returnId;
            StorageKey = storageKey;
            SHA256Hash = sha256Hash;
            ContentType = contentType;
            ByteSize = byteSize;
            SortOrder = sortOrder;
            CapturedAtUtc = capturedAtUtc;
        }

        public static PackageReturnImage Create(
            Guid returnId,
            string storageKey,
            string sha256Hash,
            string contentType,
            long byteSize,
            int sortOrder,
            DateTime capturedAtUtc)
        {
            if (returnId == Guid.Empty)
                throw new ArgumentException("Return id is required.", nameof(returnId));
            if (string.IsNullOrWhiteSpace(storageKey))
                throw new ArgumentException("Storage key is required.", nameof(storageKey));
            if (string.IsNullOrWhiteSpace(sha256Hash))
                throw new ArgumentException("SHA-256 hash is required.", nameof(sha256Hash));
            if (byteSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(byteSize), "Byte size must be positive.");

            return new PackageReturnImage(
                returnId,
                storageKey.Trim(),
                sha256Hash.Trim(),
                string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType.Trim(),
                byteSize,
                sortOrder,
                capturedAtUtc);
        }
    }
}
