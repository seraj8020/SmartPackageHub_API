using SmartPackageHub.Common.Services.Storage.Abstractions;
using SmartPackageHub.Common.Services.Storage.DTOs;

namespace HubManagement.Application.HubResidents.Services
{
    internal static class ResidentDamagePhotoUrls
    {
        public static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Authenticated proxy path used when the store cannot mint a public signed URL (local disk).
        /// </summary>
        public static Uri AuthenticatedProxyPath(Guid residentId, Guid packageId) =>
            new($"/api/v2/residents/{residentId:D}/packages/{packageId:D}/damage-photo", UriKind.Relative);

        public static async Task<(Uri? Url, DateTime? ExpiresAtUtc)> ResolveAsync(
            IParcelPhotoStore photos,
            Guid residentId,
            Guid packageId,
            string? damageImagePath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(damageImagePath))
                return (null, null);

            var signed = await photos.CreateReadUrlAsync(
                    damageImagePath,
                    DefaultLifetime,
                    cancellationToken)
                .ConfigureAwait(false);

            if (signed is ParcelPhotoReadUrl readUrl)
                return (readUrl.Url, readUrl.ExpiresAtUtc);

            return (AuthenticatedProxyPath(residentId, packageId), null);
        }
    }
}
