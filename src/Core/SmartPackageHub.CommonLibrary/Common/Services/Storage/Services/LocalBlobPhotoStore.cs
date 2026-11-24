using SmartPackageHub.Common.Services.Storage.Abstractions;
using SmartPackageHub.Common.Services.Storage.DTOs;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace SmartPackageHub.Common.Services.Storage.Services
{
    /// <summary>
    /// Local disk blob store under {ContentRoot}/App_Data.
    /// Arrival photos → <c>arrival/</c>; damage photos → <c>damage/</c>.
    /// </summary>
    public sealed class LocalBlobPhotoStore : IParcelPhotoStore
    {
        private readonly string _root;

        public LocalBlobPhotoStore(IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(environment);
            _root = Path.Combine(environment.ContentRootPath, "App_Data");
            Directory.CreateDirectory(_root);
            Directory.CreateDirectory(PhysicalPath(PhotoStoragePaths.ArrivalDirectory));
            Directory.CreateDirectory(PhysicalPath(PhotoStoragePaths.DamageDirectory));
        }

        public async Task<StoredParcelPhoto> SaveAsync(
            string clientPhotoId,
            ReadOnlyMemory<byte> bytes,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var id = RequireGuid(clientPhotoId);
            var existing = await FindAsync(id, cancellationToken).ConfigureAwait(false);
            if (existing != null)
                return existing;

            // Normal / undamaged arrival path — never writes under damage/.
            var key = PhotoStoragePaths.ArrivalBlob(id);
            var path = PhysicalPath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, bytes.ToArray(), cancellationToken).ConfigureAwait(false);
            var stored = new StoredParcelPhoto(
                id,
                key,
                Convert.ToHexString(SHA256.HashData(bytes.Span)),
                string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType,
                bytes.Length);
            await File.WriteAllTextAsync(path + ".meta.json", JsonSerializer.Serialize(stored), cancellationToken)
                .ConfigureAwait(false);
            return stored;
        }

        public async Task<StoredParcelPhoto?> FindAsync(string clientPhotoId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(clientPhotoId, out var id))
                return null;

            var guid = id.ToString("D");
            foreach (var metaRelative in new[]
                     {
                         PhotoStoragePaths.ArrivalMeta(guid),
                         PhotoStoragePaths.LegacyArrivalMeta(guid),
                     })
            {
                var metaPath = PhysicalPath(metaRelative);
                if (!File.Exists(metaPath))
                    continue;

                var json = await File.ReadAllTextAsync(metaPath, cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Deserialize<StoredParcelPhoto>(json);
            }

            return null;
        }

        public async Task WriteLinkAsync(
            string clientPhotoId,
            Guid intakeId,
            Guid packageId,
            CancellationToken cancellationToken = default)
        {
            var id = RequireGuid(clientPhotoId);
            var path = PhysicalPath(PhotoStoragePaths.ArrivalLink(id));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, $"{intakeId:D}|{packageId:D}", cancellationToken).ConfigureAwait(false);
        }

        public async Task<(Guid IntakeId, Guid PackageId)?> ReadLinkAsync(
            string clientPhotoId,
            CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(clientPhotoId, out var id))
                return null;

            var guid = id.ToString("D");
            foreach (var linkRelative in new[]
                     {
                         PhotoStoragePaths.ArrivalLink(guid),
                         PhotoStoragePaths.LegacyArrivalLink(guid),
                     })
            {
                var path = PhysicalPath(linkRelative);
                if (!File.Exists(path))
                    continue;

                var text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                var parts = text.Split('|');
                if (parts.Length != 2 ||
                    !Guid.TryParse(parts[0], out var intakeId) ||
                    !Guid.TryParse(parts[1], out var packageId))
                {
                    return null;
                }

                return (intakeId, packageId);
            }

            return null;
        }

        public async Task<string> SaveDamageAsync(
            Guid intakeId,
            ReadOnlyMemory<byte> bytes,
            CancellationToken cancellationToken = default)
        {
            // Damaged packages only — separate directory from arrival/.
            var key = PhotoStoragePaths.DamageBlob(intakeId);
            var path = PhysicalPath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, bytes.ToArray(), cancellationToken).ConfigureAwait(false);
            return key;
        }

        public async Task<string> SaveReturnAsync(
            Guid returnId,
            int sortOrder,
            ReadOnlyMemory<byte> bytes,
            CancellationToken cancellationToken = default)
        {
            var key = $"returns/{returnId:N}/{sortOrder}.jpg";
            var path = PhysicalPath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, bytes.ToArray(), cancellationToken).ConfigureAwait(false);
            return key;
        }

        public async Task<string> SaveHandoverAsync(
            Guid deliveryId,
            string kind,
            ReadOnlyMemory<byte> bytes,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(contentType);
            var safeKind = string.IsNullOrWhiteSpace(kind) ? "photo" : kind.Trim().ToUpperInvariant();
            var ext = HandoverFileExtension(contentType);
            var key = $"handover/{deliveryId:N}/{safeKind}.{ext}";
            var path = PhysicalPath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, bytes.ToArray(), cancellationToken).ConfigureAwait(false);
            return key;
        }

        private static string HandoverFileExtension(string contentType)
        {
            if (contentType.Contains("png", StringComparison.OrdinalIgnoreCase))
                return "png";
            if (contentType.Contains("svg", StringComparison.OrdinalIgnoreCase))
                return "svg";
            return "jpg";
        }

        public Task<ParcelPhotoReadUrl?> CreateReadUrlAsync(
            string storageKey,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default)
        {
            // Local disk has no public signed URL — callers use the authenticated photo proxy.
            _ = storageKey;
            _ = lifetime;
            _ = cancellationToken;
            return Task.FromResult<ParcelPhotoReadUrl?>(null);
        }

        public async Task<byte[]?> ReadBytesAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
                return null;

            var path = PhysicalPath(storageKey.Trim());
            if (!File.Exists(path))
                return null;

            return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
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
