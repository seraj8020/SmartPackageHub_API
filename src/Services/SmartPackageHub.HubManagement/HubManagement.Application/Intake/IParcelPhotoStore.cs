namespace HubManagement.Application.Intake
{
    public interface IParcelPhotoStore
    {
        Task<StoredParcelPhoto> SaveAsync(string clientPhotoId, ReadOnlyMemory<byte> bytes, string contentType, CancellationToken cancellationToken = default);
        Task<StoredParcelPhoto?> FindAsync(string clientPhotoId, CancellationToken cancellationToken = default);
        Task WriteLinkAsync(string clientPhotoId, Guid intakeId, Guid packageId, CancellationToken cancellationToken = default);
        Task<(Guid IntakeId, Guid PackageId)?> ReadLinkAsync(string clientPhotoId, CancellationToken cancellationToken = default);
        Task<string> SaveDamageAsync(Guid intakeId, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default);
    }
}
