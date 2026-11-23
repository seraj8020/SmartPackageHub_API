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
    }
}
