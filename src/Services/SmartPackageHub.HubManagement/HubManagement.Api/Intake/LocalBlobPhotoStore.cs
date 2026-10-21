using System.Security.Cryptography;
using System.Text.Json;
using HubManagement.Application.Intake;

namespace SmartPackageHub.HubManagement.Api.Intake
{
    internal class LocalBlobPhotoStore : IParcelPhotoStore
    {
        private readonly string _root;

        public LocalBlobPhotoStore(IHostEnvironment environment)
        {
            _root = Path.Combine(environment.ContentRootPath, "App_Data");
        }

        public async Task<StoredParcelPhoto> SaveAsync(string clientPhotoId, ReadOnlyMemory<byte> bytes, string contentType, CancellationToken cancellationToken = default)
        {
            var id = RequireGuid(clientPhotoId);
            var existing = await FindAsync(id, cancellationToken).ConfigureAwait(false);
            if (existing != null)
                return existing;

            var key = $"parcel-blobs/{id}.jpg";
            var path = PhysicalPath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, bytes.ToArray(), cancellationToken).ConfigureAwait(false);
            var stored = new StoredParcelPhoto(id, key, Convert.ToHexString(SHA256.HashData(bytes.ToArray())), string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType, bytes.Length);
            await File.WriteAllTextAsync(path + ".meta.json", JsonSerializer.Serialize(stored), cancellationToken).ConfigureAwait(false);
            return stored;
        }

        public async Task<StoredParcelPhoto?> FindAsync(string clientPhotoId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(clientPhotoId, out var id))
                return null;

            var metaPath = PhysicalPath($"parcel-blobs/{id:D}.jpg") + ".meta.json";
            if (!File.Exists(metaPath))
                return null;

            var json = await File.ReadAllTextAsync(metaPath, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<StoredParcelPhoto>(json);
        }

        public async Task WriteLinkAsync(string clientPhotoId, Guid intakeId, Guid packageId, CancellationToken cancellationToken = default)
        {
            var id = RequireGuid(clientPhotoId);
            var path = PhysicalPath($"parcel-blobs/{id}.link");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, $"{intakeId:D}|{packageId:D}", cancellationToken).ConfigureAwait(false);
        }

        public async Task<(Guid IntakeId, Guid PackageId)?> ReadLinkAsync(string clientPhotoId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(clientPhotoId, out var id))
                return null;

            var path = PhysicalPath($"parcel-blobs/{id:D}.link");
            if (!File.Exists(path))
                return null;

            var text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            var parts = text.Split('|');
            if (parts.Length != 2 || !Guid.TryParse(parts[0], out var intakeId) || !Guid.TryParse(parts[1], out var packageId))
                return null;

            return (intakeId, packageId);
        }

        public async Task<string> SaveDamageAsync(Guid intakeId, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        {
            var key = $"damage/{intakeId:N}.jpg";
            var path = PhysicalPath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, bytes.ToArray(), cancellationToken).ConfigureAwait(false);
            return key;
        }

        private string PhysicalPath(string key) =>
            Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));

        private static string RequireGuid(string clientPhotoId)
        {
            if (!Guid.TryParse(clientPhotoId, out var id))
                throw new InvalidOperationException("Photo id is not valid");

            return id.ToString("D");
        }
    }
}
