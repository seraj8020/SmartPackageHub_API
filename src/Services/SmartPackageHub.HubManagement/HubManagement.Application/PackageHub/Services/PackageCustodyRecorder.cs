using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.PackageHub.Services
{
    /// <summary>Writes chain-of-custody hops and resolves staff display names.</summary>
    public static class PackageCustodyRecorder
    {
        public static string TowerCageZone(string? towerWing)
        {
            var tower = string.IsNullOrWhiteSpace(towerWing) ? "Tower" : towerWing.Trim();
            return $"{tower} cage";
        }

        public static string DoorZone(string? towerWing, string? flatNumber)
        {
            var tower = string.IsNullOrWhiteSpace(towerWing) ? "Tower" : towerWing.Trim();
            var flat = string.IsNullOrWhiteSpace(flatNumber) ? "door" : flatNumber.Trim();
            return $"{tower} · {flat}";
        }

        /// <summary>Door-route waypoint: tower only, or tower + floor (never hub counter).</summary>
        public static string TowerFloorZone(string? towerWing, int? floor)
        {
            var tower = string.IsNullOrWhiteSpace(towerWing) ? "Tower" : towerWing.Trim();
            return floor is int f ? $"{tower} · Floor {f}" : tower;
        }

        public static string ResolveShelfZone(Shelf shelf)
        {
            ArgumentNullException.ThrowIfNull(shelf);

            if (!string.IsNullOrWhiteSpace(shelf.TowerOrWing))
            {
                var tower = shelf.TowerOrWing.Trim();
                if (string.Equals(tower, PackageCustodyHopKinds.DefaultHubZone, StringComparison.OrdinalIgnoreCase))
                    return PackageCustodyHopKinds.DefaultHubZone;
                return TowerCageZone(tower);
            }

            return string.IsNullOrWhiteSpace(shelf.Location?.Zone)
                ? PackageCustodyHopKinds.DefaultHubZone
                : shelf.Location.Zone.Trim();
        }

        public static async Task<string?> ResolveUsernameAsync(
            IRepository<User> users,
            Guid? staffUserId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(users);
            if (staffUserId is null || staffUserId == Guid.Empty)
                return null;

            var user = await users.GetByIdAsync(staffUserId.Value).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(user?.Username) ? null : user.Username.Trim();
        }

        public static string? LatestZone(IRepository<PackageCustodyHop> hops, Guid packageId)
        {
            ArgumentNullException.ThrowIfNull(hops);
            var last = hops.FetchMulti(h => h.PackageId == packageId, noTracking: true)
                .OrderByDescending(h => h.OccurredAtUtc)
                .ThenByDescending(h => h.Id)
                .FirstOrDefault();
            return last?.ToZone ?? last?.FromZone;
        }

        public static PackageCustodyHop Insert(
            IRepository<PackageCustodyHop> hops,
            Guid packageId,
            Guid societyId,
            string kind,
            DateTime occurredAtUtc,
            string? fromZone = null,
            string? toZone = null,
            string? locationLabel = null,
            Guid? staffUserId = null,
            string? staffUsername = null,
            string? notes = null)
        {
            ArgumentNullException.ThrowIfNull(hops);
            var hop = PackageCustodyHop.Create(
                packageId,
                societyId,
                kind,
                occurredAtUtc,
                fromZone,
                toZone,
                locationLabel,
                staffUserId,
                staffUsername,
                notes);
            hops.Insert(hop);
            return hop;
        }

        public static PackageCustodyHop RecordReceivedAtHub(
            IRepository<PackageCustodyHop> hops,
            Package package,
            string? staffUsername,
            DateTime? at = null)
        {
            ArgumentNullException.ThrowIfNull(package);
            return Insert(
                hops,
                package.Id,
                package.SocietyId,
                PackageCustodyHopKinds.ReceivedAtHub,
                at ?? package.RecievedAt,
                fromZone: null,
                toZone: PackageCustodyHopKinds.DefaultHubZone,
                locationLabel: null,
                staffUserId: package.StaffUserId,
                staffUsername: staffUsername);
        }

        public static PackageCustodyHop RecordShelved(
            IRepository<PackageCustodyHop> hops,
            Package package,
            string shelfCode,
            string? zone,
            Guid? staffUserId,
            string? staffUsername,
            DateTime? at = null)
        {
            ArgumentNullException.ThrowIfNull(package);
            var toZone = string.IsNullOrWhiteSpace(zone)
                ? PackageCustodyHopKinds.DefaultHubZone
                : zone.Trim();
            return Insert(
                hops,
                package.Id,
                package.SocietyId,
                PackageCustodyHopKinds.Shelved,
                at ?? package.ShelfAssignedAtUtc ?? DateTime.UtcNow,
                fromZone: PackageCustodyHopKinds.DefaultHubZone,
                toZone: toZone,
                locationLabel: shelfCode,
                staffUserId: staffUserId ?? package.StaffUserId,
                staffUsername: staffUsername);
        }
    }
}
