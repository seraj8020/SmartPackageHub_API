using HubManagement.Application.HubResidents.Services;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Services
{
    /// <summary>
    /// Batch HasArrivalPhoto for staff society package lists.
    /// One metadata query only — never downloads image bytes.
    /// </summary>
    internal static class SocietyArrivalPhotoFlags
    {
        private static readonly Action<ILogger, int, Exception?> LogFlagsFailed =
            LoggerMessage.Define<int>(
                LogLevel.Warning,
                new EventId(1, nameof(SocietyArrivalPhotoFlags)),
                "Could not resolve arrival-photo flags for {PackageCount} society packages.");

        public static async Task<HashSet<Guid>> ResolveSafeAsync(
            IRepository<PackageImage> images,
            IReadOnlyCollection<Guid> packageIds,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            if (packageIds.Count == 0)
                return [];

            try
            {
                var idSet = packageIds as HashSet<Guid> ?? packageIds.ToHashSet();
                var rows = await images.ListAsync(
                        image => idSet.Contains(image.PackageId),
                        cancellationToken: cancellationToken,
                        noTracking: true)
                    .ConfigureAwait(false);
                return ResidentArrivalPhotoLookup.PackageIdsWithArrivalPhoto(rows, idSet);
            }
#pragma warning disable CA1031 // Staff lists must still return packages if photo flag lookup fails.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogFlagsFailed(logger, packageIds.Count, ex);
                return [];
            }
        }
    }
}
