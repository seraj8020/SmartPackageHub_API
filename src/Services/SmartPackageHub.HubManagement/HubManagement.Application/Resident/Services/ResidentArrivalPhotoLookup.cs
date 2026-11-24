using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Services
{
    /// <summary>Finds the hub arrival / parcel photo stored at intake (Kind = Parcel).</summary>
    internal static class ResidentArrivalPhotoLookup
    {
        private static readonly HashSet<string> ArrivalKinds = new(StringComparer.OrdinalIgnoreCase)
        {
            "Parcel",
            "Label",
            "Front",
            "Arrival",
        };

        public static PackageImage? FindForPackage(IRepository<PackageImage> images, Guid packageId)
        {
            // Load by package id only (EF-safe), then filter Kind/StorageKey in memory.
            // HashSet.Contains / StringComparison in IQueryable caused HTTP 500 on QA.
            return images
                .FetchMulti(image => image.PackageId == packageId, noTracking: true)
                .AsEnumerable()
                .Where(IsArrivalImage)
                .OrderByDescending(image => image.CapturedAt)
                .FirstOrDefault();
        }

        public static HashSet<Guid> PackageIdsWithArrivalPhoto(
            IReadOnlyList<PackageImage> images,
            IReadOnlyCollection<Guid> packageIds)
        {
            var wanted = packageIds as HashSet<Guid> ?? packageIds.ToHashSet();
            return images
                .Where(image => wanted.Contains(image.PackageId) && IsArrivalImage(image))
                .Select(image => image.PackageId)
                .ToHashSet();
        }

        public static bool IsArrivalImage(PackageImage image) =>
            image is not null &&
            !string.IsNullOrWhiteSpace(image.StorageKey) &&
            !string.IsNullOrWhiteSpace(image.Kind) &&
            ArrivalKinds.Contains(image.Kind);
    }
}
