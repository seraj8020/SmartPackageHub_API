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
            return images
                .FetchMulti(image => image.PackageId == packageId, noTracking: true)
                .Where(image =>
                    !string.IsNullOrWhiteSpace(image.StorageKey) &&
                    ArrivalKinds.Contains(image.Kind))
                .OrderByDescending(image => image.CapturedAt)
                .FirstOrDefault();
        }
    }
}
