using SmartPackageHub.Common.Services.Storage.DTOs;

namespace SmartPackageHub.Common.Services.Storage.Abstractions
{
    public interface IParcelPhotoStore
    {
        Task<StoredParcelPhoto> SaveAsync(string clientPhotoId, ReadOnlyMemory<byte> bytes, string contentType, CancellationToken cancellationToken = default);
        Task<StoredParcelPhoto?> FindAsync(string clientPhotoId, CancellationToken cancellationToken = default);
        Task WriteLinkAsync(string clientPhotoId, Guid intakeId, Guid packageId, CancellationToken cancellationToken = default);
        Task<(Guid IntakeId, Guid PackageId)?> ReadLinkAsync(string clientPhotoId, CancellationToken cancellationToken = default);
        Task<string> SaveDamageAsync(Guid intakeId, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default);

        /// <summary>Stores a resident return photo under returns/{returnId}/{sortOrder}.jpg.</summary>
        Task<string> SaveReturnAsync(
            Guid returnId,
            int sortOrder,
            ReadOnlyMemory<byte> bytes,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores proof-of-delivery media under handover/{deliveryId}/{kind}.{ext}
        /// (kind typically photo or signature).
        /// </summary>
        Task<string> SaveHandoverAsync(
            Guid deliveryId,
            string kind,
            ReadOnlyMemory<byte> bytes,
            string contentType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Time-limited read URL for a stored object key (e.g. damage/{intakeId}.jpg).
        /// Returns null when the provider cannot mint a public URL (local disk) — callers should use an authenticated proxy.
        /// </summary>
        Task<ParcelPhotoReadUrl?> CreateReadUrlAsync(
            string storageKey,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default);

        /// <summary>Reads raw bytes for a stored object key when a signed URL is unavailable (local/dev).</summary>
        Task<byte[]?> ReadBytesAsync(string storageKey, CancellationToken cancellationToken = default);
    }
}
