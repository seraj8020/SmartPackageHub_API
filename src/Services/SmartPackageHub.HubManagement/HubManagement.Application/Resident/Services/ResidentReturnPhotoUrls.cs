using SmartPackageHub.Common.Services.Storage.Abstractions;
using SmartPackageHub.Common.Services.Storage.DTOs;

namespace HubManagement.Application.HubResidents.Services
{
    internal static class ResidentReturnPhotoUrls
    {
        public static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(15);

        public static Uri AuthenticatedProxyPath(Guid residentId, Guid packageId, Guid photoId) =>
            new(
                $"/api/v2/residents/{residentId:D}/packages/{packageId:D}/return-photos/{photoId:D}",
                UriKind.Relative);

        public static async Task<(Uri? Url, DateTime? ExpiresAtUtc)> ResolveAsync(
            IParcelPhotoStore photos,
            Guid residentId,
            Guid packageId,
            Guid photoId,
            string? storageKey,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
                return (null, null);

            var signed = await photos.CreateReadUrlAsync(
                    storageKey,
                    DefaultLifetime,
                    cancellationToken)
                .ConfigureAwait(false);

            if (signed is ParcelPhotoReadUrl readUrl)
                return (readUrl.Url, readUrl.ExpiresAtUtc);

            return (AuthenticatedProxyPath(residentId, packageId, photoId), null);
        }
    }
}
