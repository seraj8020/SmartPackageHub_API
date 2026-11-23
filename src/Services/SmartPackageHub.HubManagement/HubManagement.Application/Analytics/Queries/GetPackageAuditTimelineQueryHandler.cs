using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class GetPackageAuditTimelineQueryHandler
        : QueryHandler<GetPackageAuditTimelineQuery, PackageAuditTimelineDto?>
    {
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<PackageCustodyHop> _custodyHops;
        private readonly IRepository<Delivery> _deliveries;

        public GetPackageAuditTimelineQueryHandler(
            IMapper mapper,
            IRepository<Package> packages,
            IRepository<Society> societies,
            IRepository<PackageCustodyHop> custodyHops,
            IRepository<Delivery> deliveries) : base(mapper)
        {
            _packages = packages;
            _societies = societies;
            _custodyHops = custodyHops;
            _deliveries = deliveries;
        }

        protected override async Task<PackageAuditTimelineDto?> HandleAsync(GetPackageAuditTimelineQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            if (package is null)
                return null;

            if (request.SocietyId is Guid societyId
                && societyId != Guid.Empty
                && package.SocietyId != societyId)
            {
                return null;
            }

            var society = await _societies.GetByIdAsync(package.SocietyId).ConfigureAwait(false);
            var deliveredAt = _deliveries.FetchMulti(
                    d => d.PackageId == package.Id && d.Status.Value == "Completed",
                    noTracking: true)
                .AsEnumerable()
                .Select(d => (DateTime?)d.DeliveredAt)
                .DefaultIfEmpty(null)
                .Max();

            var hops = _custodyHops.FetchMulti(h => h.PackageId == package.Id, noTracking: true)
                .AsEnumerable()
                .OrderBy(h => h.OccurredAtUtc)
                .ThenBy(h => h.Id)
                .ToList();

            var events = hops.Select(hop => new RecentActivityItemDto(
                    hop.OccurredAtUtc,
                    hop.Kind,
                    PackageCustodyActivityFormatter.Format(hop, package.PkgNumber),
                    hop.SocietyId,
                    society?.Name,
                    hop.PackageId,
                    hop.FromZone,
                    hop.ToZone,
                    hop.LocationLabel,
                    hop.StaffUsername,
                    package.PkgNumber))
                .ToList();

            // Older packages with no hops: synthesize receive (+ shelve if known).
            if (events.Count == 0)
            {
                events.Add(new RecentActivityItemDto(
                    package.RecievedAt,
                    PackageCustodyHopKinds.ReceivedAtHub,
                    $"{package.PkgNumber} received at {society?.Name ?? PackageCustodyHopKinds.DefaultHubZone}",
                    package.SocietyId,
                    society?.Name,
                    package.Id,
                    null,
                    PackageCustodyHopKinds.DefaultHubZone,
                    null,
                    null,
                    package.PkgNumber));

                if (package.ShelfAssignedAtUtc is DateTime shelfAt)
                {
                    events.Add(new RecentActivityItemDto(
                        shelfAt,
                        PackageCustodyHopKinds.Shelved,
                        $"{package.PkgNumber} shelved",
                        package.SocietyId,
                        society?.Name,
                        package.Id,
                        PackageCustodyHopKinds.DefaultHubZone,
                        package.TowerWing,
                        null,
                        null,
                        package.PkgNumber));
                }

                if (deliveredAt is DateTime delivered)
                {
                    events.Add(new RecentActivityItemDto(
                        delivered,
                        PackageCustodyHopKinds.Delivered,
                        $"{package.PkgNumber} delivered",
                        package.SocietyId,
                        society?.Name,
                        package.Id,
                        null,
                        null,
                        package.FlatNumber.Value,
                        null,
                        package.PkgNumber));
                }
            }

            return new PackageAuditTimelineDto
            {
                Package = new AuditPackageSearchItemDto
                {
                    Id = package.Id,
                    PackageNumber = package.PkgNumber,
                    TrackingNumber = package.SphTrackingNumber?.Value,
                    CourierAwb = package.CourierAwb,
                    RecipientName = package.RecipientName,
                    Courier = package.CourierPartner,
                    Status = package.Status.ToString(),
                    ReceivedAtUtc = package.RecievedAt,
                    DeliveredAtUtc = package.Status == PackageStatus.Delivered ? deliveredAt : null,
                    TowerWing = package.TowerWing,
                    FlatNumber = package.FlatNumber.Value,
                    SocietyId = package.SocietyId,
                    SocietyName = society?.Name,
                },
                Events = events,
            };
        }
    }
}