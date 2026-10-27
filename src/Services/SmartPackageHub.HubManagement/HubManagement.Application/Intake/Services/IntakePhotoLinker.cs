using SmartPackageHub.Common.Services.Storage.Abstractions;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Repositories;

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
            var saved = await photos.FindAsync(clientPhotoId, cancellationToken).ConfigureAwait(false) ;
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

            var exists = images.GetAll().Any(image => image.IntakeId == resolvedIntake && image.StorageKey == saved.StorageKey);
            var inserted = false;
            if (!exists)
            {
                var image = PackageImage.Create(resolvedPackage, resolvedIntake, "Parcel", saved.StorageKey, saved.Sha256, DateTime.UtcNow);
                image.ContentType = saved.ContentType;
                image.ByteSize = saved.ByteSize;
                images.Insert(image);
                inserted = true;
            }

            await photos.WriteLinkAsync(clientPhotoId, resolvedIntake, resolvedPackage, cancellationToken).ConfigureAwait(false);
            return (true, inserted);
        }
    }
}
