using SmartPackageHub.Common.Services.Storage.Abstractions;
using SmartPackageHub.Common.Services.Storage.DTOs;

namespace HubManagement.Application.HubResidents.Services
{
    internal static class ResidentArrivalPhotoUrls
    {
        public static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Authenticated proxy path used when the store cannot mint a public signed URL (local disk).
        /// </summary>
        public static Uri AuthenticatedProxyPath(Guid residentId, Guid packageId) =>
            new($"/api/v2/residents/{residentId:D}/packages/{packageId:D}/arrival-photo", UriKind.Relative);

        public static async Task<(Uri? Url, DateTime? ExpiresAtUtc)> ResolveAsync(
            IParcelPhotoStore photos,
            Guid residentId,
            Guid packageId,
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

            return (AuthenticatedProxyPath(residentId, packageId), null);
        }
    }
}
