using System.Security.Cryptography;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;
using SmartPackageHub.Common.Services.Storage.DTOs;
using SmartPackageHub.Common.Services.Storage.Services;

namespace HubManagement.Application.Intake.Services
{
    internal static class IntakePhotoLinker
    {
        public static async Task<(bool Linked, bool Inserted)> LinkAsync(
            IParcelPhotoStore photos,
            IRepository<PackageImage> images,
            string clientPhotoId,
            Guid? intakeId,
            Guid? packageId,
            CancellationToken cancellationToken)
        {
            var saved = await ResolveStoredPhotoAsync(photos, clientPhotoId, cancellationToken)
                .ConfigureAwait(false);
            if (saved == null)
                return (false, false);

            Guid resolvedIntake;
            Guid resolvedPackage;
            if (intakeId is Guid knownIntake && packageId is Guid knownPackage)
            {
                resolvedIntake = knownIntake;
                resolvedPackage = knownPackage;
            }
            else
            {
                var link = await photos.ReadLinkAsync(clientPhotoId, cancellationToken).ConfigureAwait(false);
                if (link == null)
                    return (false, false);

                resolvedIntake = link.Value.IntakeId;
                resolvedPackage = link.Value.PackageId;
            }

            // Scoped query — never GetAll() (scanned every PackageImage row and slowed intake).
            var storageKey = saved.StorageKey;
            var exists = images
                .FetchMulti(
                    image => image.IntakeId == resolvedIntake && image.StorageKey == storageKey,
                    noTracking: true)
                .Any();
            var inserted = false;
            if (!exists)
            {
                var image = PackageImage.Create(
                    resolvedPackage,
                    resolvedIntake,
                    "Parcel",
                    saved.StorageKey,
                    saved.Sha256,
                    DateTime.UtcNow);
                image.ContentType = string.IsNullOrWhiteSpace(saved.ContentType)
                    ? "image/jpeg"
                    : saved.ContentType;
                image.ByteSize = saved.ByteSize;
                images.Insert(image);
                inserted = true;
            }

            await photos.WriteLinkAsync(clientPhotoId, resolvedIntake, resolvedPackage, cancellationToken)
                .ConfigureAwait(false);
            return (true, inserted);
        }

        /// <summary>
        /// Prefer meta.json via FindAsync; if meta is missing/corrupt but the JPEG exists
        /// (common with GCS), still build a StoredParcelPhoto from the blob bytes.
        /// </summary>
        private static async Task<StoredParcelPhoto?> ResolveStoredPhotoAsync(
            IParcelPhotoStore photos,
            string clientPhotoId,
            CancellationToken cancellationToken)
        {
            try
            {
                var saved = await photos.FindAsync(clientPhotoId, cancellationToken).ConfigureAwait(false);
                if (saved is not null && !string.IsNullOrWhiteSpace(saved.StorageKey))
                    return saved;
            }
#pragma warning disable CA1031 // Fall back to raw blob when meta is unreadable.
            catch
#pragma warning restore CA1031
            {
                // meta.json missing or invalid JSON — try the blob itself.
            }

            if (!Guid.TryParse(clientPhotoId, out var photoId))
                return null;

            // Do not download the full JPEG just to build a link row — meta miss is common on GCS.
            // Confirm only runs this after a successful upload for the same clientPhotoId.
            var guid = photoId.ToString("D");
            var key = PhotoStoragePaths.ArrivalBlob(guid);
            return new StoredParcelPhoto(
                guid,
                key,
                Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key))),
                "image/jpeg",
                0);
        }
    }
}
