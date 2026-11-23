using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.PackageHub.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetRecentActivityQueryHandler
        : QueryHandler<GetRecentActivityQuery, IReadOnlyList<RecentActivityItemDto>>
    {
        private readonly IRepository<Society> _societies;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Delivery> _deliveries;
        private readonly IRepository<PackageCustodyHop> _custodyHops;
        private readonly IRepository<Shelf> _shelves;

        public GetRecentActivityQueryHandler(
            IMapper mapper,
            IRepository<Society> societies,
            IRepository<Package> packages,
            IRepository<Delivery> deliveries,
            IRepository<PackageCustodyHop> custodyHops,
            IRepository<Shelf> shelves) : base(mapper)
        {
            _societies = societies;
            _packages = packages;
            _deliveries = deliveries;
            _custodyHops = custodyHops;
            _shelves = shelves;
        }

        protected override Task<IReadOnlyList<RecentActivityItemDto>> HandleAsync(GetRecentActivityQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var take = Math.Clamp(request.Take, 1, 100);
            var societyFilter = request.SocietyId is Guid sid && sid != Guid.Empty;

            var societyNames = _societies.GetAll()
                .Where(s => !societyFilter || s.Id == request.SocietyId!.Value)
                .ToDictionary(s => s.Id, s => s.Name);

            var items = new List<RecentActivityItemDto>();

            AppendCustodyHops(items, societyFilter, request.SocietyId, take, societyNames);
            AppendSocietyOnboards(items, societyFilter, request.SocietyId, take);
            AppendLegacyLifecycle(items, societyFilter, request.SocietyId, take, societyNames);

            var result = items
                .OrderByDescending(i => i.OccurredAtUtc)
                .Take(take)
                .ToList();

            return Task.FromResult<IReadOnlyList<RecentActivityItemDto>>(result);
        }

        private void AppendCustodyHops(
            List<RecentActivityItemDto> items,
            bool societyFilter,
            Guid? societyId,
            int take,
            Dictionary<Guid, string> societyNames)
        {
            var hops = _custodyHops.GetAll()
                .Where(h => !societyFilter || h.SocietyId == societyId!.Value)
                .OrderByDescending(h => h.OccurredAtUtc)
                .ThenByDescending(h => h.Id)
                .Take(take)
                .ToList();

            if (hops.Count == 0)
                return;

            var packageIds = hops.Select(h => h.PackageId).Distinct().ToList();
            var packagesById = _packages.GetAll()
                .Where(p => packageIds.Contains(p.Id))
                .ToDictionary(p => p.Id);

            foreach (var hop in hops)
            {
                societyNames.TryGetValue(hop.SocietyId, out var societyName);
                packagesById.TryGetValue(hop.PackageId, out var package);
                var pkgNumber = package?.PkgNumber;
                items.Add(new RecentActivityItemDto(
                    hop.OccurredAtUtc,
                    hop.Kind,
                    FormatCustodyText(hop, pkgNumber),
                    hop.SocietyId,
                    societyName,
                    hop.PackageId,
                    hop.FromZone,
                    hop.ToZone,
                    hop.LocationLabel,
                    hop.StaffUsername,
                    pkgNumber));
            }
        }

        private void AppendSocietyOnboards(
            List<RecentActivityItemDto> items,
            bool societyFilter,
            Guid? societyId,
            int take)
        {
            foreach (var society in _societies.GetAll()
                         .Where(s => (!societyFilter || s.Id == societyId!.Value) && s.OnboardedAtUtc.HasValue)
                         .OrderByDescending(s => s.OnboardedAtUtc)
                         .ThenByDescending(s => s.Id)
                         .Take(take)
                         .ToList())
            {
                items.Add(new RecentActivityItemDto(
                    society.OnboardedAtUtc!.Value,
                    "society-onboard",
                    $"Society onboarded: {society.Name}",
                    society.Id,
                    society.Name,
                    null));
            }
        }

        private void AppendLegacyLifecycle(
            List<RecentActivityItemDto> items,
            bool societyFilter,
            Guid? societyId,
            int take,
            Dictionary<Guid, string> societyNames)
        {
            // Prefer real custody hops when present for a package; synthesize only for packages with none yet.
            var packagesWithHops = _custodyHops.GetAll()
                .Where(h => !societyFilter || h.SocietyId == societyId!.Value)
                .Select(h => h.PackageId)
                .Distinct()
                .ToHashSet();

            var packages = _packages.GetAll()
                .Where(p => !societyFilter || p.SocietyId == societyId!.Value)
                .OrderByDescending(p => p.RecievedAt)
                .ThenByDescending(p => p.Id)
                .Take(take)
                .ToList();

            var shelfIds = packages
                .Where(p => p.ShelfId.HasValue)
                .Select(p => p.ShelfId!.Value)
                .Distinct()
                .ToList();
            var shelvesById = shelfIds.Count == 0
                ? new Dictionary<Guid, Shelf>()
                : _shelves.GetAll()
                    .Where(s => shelfIds.Contains(s.Id))
                    .ToDictionary(s => s.Id);

            foreach (var package in packages)
            {
                if (packagesWithHops.Contains(package.Id))
                    continue;

                societyNames.TryGetValue(package.SocietyId, out var societyName);
                items.Add(new RecentActivityItemDto(
                    package.RecievedAt,
                    PackageCustodyHopKinds.ReceivedAtHub,
                    FormatLegacyReceived(package.PkgNumber, societyName),
                    package.SocietyId,
                    societyName,
                    package.Id,
                    null,
                    PackageCustodyHopKinds.DefaultHubZone,
                    null,
                    null,
                    package.PkgNumber));

                if (package.ShelfAssignedAtUtc is DateTime shelfAt)
                {
                    string? shelfCode = null;
                    string zone = PackageCustodyHopKinds.DefaultHubZone;
                    if (package.ShelfId is Guid shelfId && shelvesById.TryGetValue(shelfId, out var shelf))
                    {
                        shelfCode = shelf.Code;
                        zone = string.IsNullOrWhiteSpace(shelf.TowerOrWing)
                            ? (shelf.Location?.Zone ?? PackageCustodyHopKinds.DefaultHubZone)
                            : shelf.TowerOrWing.Trim();
                    }

                    items.Add(new RecentActivityItemDto(
                        shelfAt,
                        PackageCustodyHopKinds.Shelved,
                        FormatLegacyShelved(package.PkgNumber, shelfCode, zone),
                        package.SocietyId,
                        societyName,
                        package.Id,
                        PackageCustodyHopKinds.DefaultHubZone,
                        zone,
                        shelfCode,
                        null,
                        package.PkgNumber));
                }
            }

            var deliveries = _deliveries.GetAll()
                .Where(d => (!societyFilter || d.SocietyId == societyId!.Value) &&
                    d.Status.Value == "Completed")
                .OrderByDescending(d => d.DeliveredAt)
                .ThenByDescending(d => d.Id)
                .Take(take)
                .ToList();

            var deliveryPackageIds = deliveries.Select(d => d.PackageId).Distinct().ToList();
            var deliveryPackages = deliveryPackageIds.Count == 0
                ? new Dictionary<Guid, Package>()
                : _packages.GetAll()
                    .Where(p => deliveryPackageIds.Contains(p.Id))
                    .ToDictionary(p => p.Id);

            foreach (var delivery in deliveries)
            {
                if (packagesWithHops.Contains(delivery.PackageId))
                    continue;

                societyNames.TryGetValue(delivery.SocietyId, out var societyName);
                deliveryPackages.TryGetValue(delivery.PackageId, out var pkg);
                var pkgNumber = pkg?.PkgNumber ?? "package";
                var viaOtp = delivery.OtpLogId is not null ? " via OTP" : string.Empty;
                items.Add(new RecentActivityItemDto(
                    delivery.DeliveredAt,
                    PackageCustodyHopKinds.Delivered,
                    $"{pkgNumber} delivered{viaOtp} ({delivery.Method})",
                    delivery.SocietyId,
                    societyName,
                    delivery.PackageId,
                    null,
                    pkg is null ? null : PackageCustodyRecorder.DoorZone(pkg.TowerWing, pkg.FlatNumber.Value),
                    pkg?.FlatNumber.Value,
                    null,
                    pkgNumber));
            }
        }

        private static string FormatCustodyText(PackageCustodyHop hop, string? pkgNumber)
        {
            var pkg = string.IsNullOrWhiteSpace(pkgNumber) ? "Package" : pkgNumber;
            var staff = string.IsNullOrWhiteSpace(hop.StaffUsername) ? null : hop.StaffUsername;
            var staffSuffix = staff is null ? string.Empty : $", staff {staff}";

            return hop.Kind switch
            {
                PackageCustodyHopKinds.ReceivedAtHub =>
                    $"{pkg} received at {hop.ToZone ?? PackageCustodyHopKinds.DefaultHubZone}{staffSuffix}",
                PackageCustodyHopKinds.Shelved =>
                    string.IsNullOrWhiteSpace(hop.LocationLabel)
                        ? $"{pkg} shelved at {hop.ToZone ?? PackageCustodyHopKinds.DefaultHubZone}{staffSuffix}"
                        : $"{pkg} shelved at {hop.LocationLabel} ({hop.ToZone ?? PackageCustodyHopKinds.DefaultHubZone}){staffSuffix}",
                PackageCustodyHopKinds.ZoneTransfer =>
                    $"{pkg} left {hop.FromZone ?? "?"} → arrived {hop.ToZone ?? "?"}{staffSuffix}"
                    + (string.IsNullOrWhiteSpace(hop.LocationLabel) ? string.Empty : $". Shelf {hop.LocationLabel}"),
                PackageCustodyHopKinds.OutForDoorDelivery =>
                    $"{pkg} out for door delivery{(string.IsNullOrWhiteSpace(hop.LocationLabel) ? string.Empty : $" to {hop.LocationLabel}")}{staffSuffix}",
                PackageCustodyHopKinds.Delivered =>
                    $"{pkg} delivered{(string.IsNullOrWhiteSpace(hop.Notes) ? string.Empty : $" ({hop.Notes})")}{staffSuffix}",
                PackageCustodyHopKinds.DeliveryFailed =>
                    $"{pkg} delivery failed{(string.IsNullOrWhiteSpace(hop.Notes) ? string.Empty : $" — {hop.Notes}")}{staffSuffix}",
                PackageCustodyHopKinds.ReturnedToCage =>
                    $"{pkg} returned to {hop.ToZone ?? "cage"}{staffSuffix}",
                _ => $"{pkg} {hop.Kind}{staffSuffix}",
            };
        }

        private static string FormatLegacyReceived(string pkgNumber, string? societyName) =>
            $"{pkgNumber} received at {societyName ?? PackageCustodyHopKinds.DefaultHubZone}";

        private static string FormatLegacyShelved(string pkgNumber, string? shelfCode, string zone) =>
            string.IsNullOrWhiteSpace(shelfCode)
                ? $"{pkgNumber} shelved at {zone}"
                : $"{pkgNumber} shelved at {shelfCode} ({zone})";
    }
}
