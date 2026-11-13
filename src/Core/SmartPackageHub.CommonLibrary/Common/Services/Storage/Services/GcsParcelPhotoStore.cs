using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Common.Services.Storage.Abstractions;
using SmartPackageHub.Common.Services.Storage.DTOs;
using SmartPackageHub.Common.Services.Storage.Options;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace SmartPackageHub.Common.Services.Storage.Services
{
    /// <summary>
    /// Google Cloud Storage backend for parcel label photos and damage shots.
    /// Object layout mirrors the local store: parcel-blobs/*, damage/*.
    /// </summary>
    public sealed class GcsParcelPhotoStore : IParcelPhotoStore
    {
        private static readonly Action<ILogger, string, string, Exception?> LogUploaded =
            LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                new EventId(1, nameof(GcsParcelPhotoStore)),
                "Uploaded photo object {ObjectName} to bucket {Bucket}");

        private readonly StorageClient _storage;
        private readonly string _bucket;
        private readonly string _prefix;
        private readonly ILogger<GcsParcelPhotoStore> _logger;

        public GcsParcelPhotoStore(
            IOptions<ParcelPhotoStorageOptions> options,
            ILogger<GcsParcelPhotoStore> logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            var settings = options.Value;
            if (string.IsNullOrWhiteSpace(settings.Bucket))
                throw new InvalidOperationException(
                    "ParcelPhotoStorage:Bucket is required when Provider=Gcs.");

            _bucket = settings.Bucket.Trim();
            _prefix = NormalizePrefix(settings.ObjectPrefix);
            _logger = logger;
            // Uses Application Default Credentials (VM SA, GOOGLE_APPLICATION_CREDENTIALS, etc.).
            _storage = StorageClient.Create();
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

            var key = $"parcel-blobs/{id}.jpg";
            var objectName = ObjectName(key);
            var payload = bytes.ToArray();
            var type = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType;

            await UploadBytesAsync(objectName, payload, type, cancellationToken).ConfigureAwait(false);

            var stored = new StoredParcelPhoto(
                id,
                key,
                Convert.ToHexString(SHA256.HashData(payload)),
                type,
                payload.Length);

            var metaBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(stored));
            await UploadBytesAsync(objectName + ".meta.json", metaBytes, "application/json", cancellationToken)
                .ConfigureAwait(false);

            LogUploaded(_logger, objectName, _bucket, null);
            return stored;
        }

        public async Task<StoredParcelPhoto?> FindAsync(
            string clientPhotoId,
            CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(clientPhotoId, out var id))
                return null;

            var metaName = ObjectName($"parcel-blobs/{id:D}.jpg") + ".meta.json";
            try
            {
                var stream = new MemoryStream();
                await using (stream.ConfigureAwait(false))
                {
                    await _storage.DownloadObjectAsync(_bucket, metaName, stream, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    stream.Position = 0;
                    return await JsonSerializer.DeserializeAsync<StoredParcelPhoto>(stream, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task WriteLinkAsync(
            string clientPhotoId,
            Guid intakeId,
            Guid packageId,
            CancellationToken cancellationToken = default)
        {
            var id = RequireGuid(clientPhotoId);
            var objectName = ObjectName($"parcel-blobs/{id}.link");
            var payload = Encoding.UTF8.GetBytes($"{intakeId:D}|{packageId:D}");
            await UploadBytesAsync(objectName, payload, "text/plain", cancellationToken).ConfigureAwait(false);
        }

        public async Task<(Guid IntakeId, Guid PackageId)?> ReadLinkAsync(
            string clientPhotoId,
            CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(clientPhotoId, out var id))
                return null;

            var objectName = ObjectName($"parcel-blobs/{id:D}.link");
            try
            {
                var stream = new MemoryStream();
                await using (stream.ConfigureAwait(false))
                {
                    await _storage.DownloadObjectAsync(_bucket, objectName, stream, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    var text = Encoding.UTF8.GetString(stream.ToArray());
                    var parts = text.Split('|');
                    if (parts.Length != 2 ||
                        !Guid.TryParse(parts[0], out var intakeId) ||
                        !Guid.TryParse(parts[1], out var packageId))
                    {
                        return null;
                    }

                    return (intakeId, packageId);
                }
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<string> SaveDamageAsync(
            Guid intakeId,
            ReadOnlyMemory<byte> bytes,
            CancellationToken cancellationToken = default)
        {
            var key = $"damage/{intakeId:N}.jpg";
            var objectName = ObjectName(key);
            await UploadBytesAsync(objectName, bytes.ToArray(), "image/jpeg", cancellationToken)
                .ConfigureAwait(false);
            LogUploaded(_logger, objectName, _bucket, null);
            return key;
        }

        private async Task UploadBytesAsync(
            string objectName,
            byte[] payload,
            string contentType,
            CancellationToken cancellationToken)
        {
            var stream = new MemoryStream(payload);
            await using (stream.ConfigureAwait(false))
            {
                var obj = new Object
                {
                    Bucket = _bucket,
                    Name = objectName,
                    ContentType = contentType,
                };
                await _storage.UploadObjectAsync(obj, stream, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private string ObjectName(string relativeKey)
        {
            var key = relativeKey.Replace('\\', '/').TrimStart('/');
            return string.IsNullOrEmpty(_prefix) ? key : _prefix + key;
        }

        private static string NormalizePrefix(string? prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return string.Empty;

            var value = prefix.Trim().Replace('\\', '/').Trim('/');
            return string.IsNullOrEmpty(value) ? string.Empty : value + "/";
        }

        private static string RequireGuid(string clientPhotoId)
        {
            if (!Guid.TryParse(clientPhotoId, out var id))
                throw new InvalidOperationException("Photo id is not valid");

            return id.ToString("D");
        }
    }
}
