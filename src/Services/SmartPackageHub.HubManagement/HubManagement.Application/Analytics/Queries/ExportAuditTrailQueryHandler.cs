using System.Globalization;
using System.Text;
using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class ExportAuditTrailQueryHandler
        : QueryHandler<ExportAuditTrailQuery, ReportsExportResult>
    {
        private const int MaxPackages = 2000;

        private readonly IRepository<Package> _packages;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<Delivery> _deliveries;
        private readonly IRepository<PackageCustodyHop> _custodyHops;

        public ExportAuditTrailQueryHandler(
            IMapper mapper,
            IRepository<Package> packages,
            IRepository<Society> societies,
            IRepository<Delivery> deliveries,
            IRepository<PackageCustodyHop> custodyHops) : base(mapper)
        {
            _packages = packages;
            _societies = societies;
            _deliveries = deliveries;
            _custodyHops = custodyHops;
        }

        protected override Task<ReportsExportResult> HandleAsync(ExportAuditTrailQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var packages = LoadMatchingPackages(request);
            var societyNames = LoadSocietyNames(packages);
            var deliveredAt = LoadDeliveredAt(packages);
            var hopsByPackage = LoadHops(packages);

            var sb = new StringBuilder();
            sb.AppendLine(
                "packageNumber,trackingNumber,courierAwb,recipientName,courier,status,receivedAtUtc,deliveredAtUtc,towerWing,flatNumber,societyName,eventAtUtc,eventKind,eventText,fromZone,toZone,locationLabel,staffUsername");

            foreach (var package in packages)
            {
                societyNames.TryGetValue(package.SocietyId, out var societyName);
                deliveredAt.TryGetValue(package.Id, out var delivered);
                DateTime? deliveredUtc = package.Status == PackageStatus.Delivered ? delivered : null;
                hopsByPackage.TryGetValue(package.Id, out var hops);
                WritePackageEvents(sb, package, societyName, deliveredUtc, hops ?? []);
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
            var fileName = request.PackageId is Guid single && single != Guid.Empty
                ? $"sph-audit-package-{stamp}.csv"
                : $"sph-audit-trail-{stamp}.csv";

            return Task.FromResult(new ReportsExportResult(fileName, "text/csv", sb.ToString()));
        }

        private List<Package> LoadMatchingPackages(ExportAuditTrailQuery request)
        {
            var societyFilter = request.SocietyId is Guid sid && sid != Guid.Empty;
            var q = string.IsNullOrWhiteSpace(request.Q) ? null : request.Q.Trim();
            var courier = string.IsNullOrWhiteSpace(request.Courier) ? null : request.Courier.Trim();

            DateTime? fromUtc = request.FromDate is DateOnly from
                ? DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
                : null;
            DateTime? toExclusiveUtc = request.ToDate is DateOnly to
                ? DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
                : null;

            IEnumerable<Package> query = _packages.GetAll()
                .Where(p => !societyFilter || p.SocietyId == request.SocietyId!.Value)
                .Where(p => fromUtc == null || p.RecievedAt >= fromUtc.Value)
                .Where(p => toExclusiveUtc == null || p.RecievedAt < toExclusiveUtc.Value)
                .AsEnumerable();

            if (request.PackageId is Guid packageId && packageId != Guid.Empty)
                query = query.Where(p => p.Id == packageId);

            if (courier is not null)
            {
                query = query.Where(p =>
                    !string.IsNullOrWhiteSpace(p.CourierPartner) &&
                    p.CourierPartner.Contains(courier, StringComparison.OrdinalIgnoreCase));
            }

            if (q is not null)
                query = query.Where(p => MatchesSearch(p, q));

            return query
                .OrderByDescending(p => p.RecievedAt)
                .ThenByDescending(p => p.Id)
                .Take(MaxPackages)
                .ToList();
        }

        private static bool MatchesSearch(Package p, string q) =>
            (!string.IsNullOrWhiteSpace(p.RecipientName) &&
             p.RecipientName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(p.CourierPartner) &&
             p.CourierPartner.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(p.PkgNumber) &&
             p.PkgNumber.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(p.CourierAwb) &&
             p.CourierAwb.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
            (p.SphTrackingNumber != null &&
             p.SphTrackingNumber.Value.Contains(q, StringComparison.OrdinalIgnoreCase));

        private Dictionary<Guid, string> LoadSocietyNames(List<Package> packages) =>
            _societies.GetAll()
                .Where(s => packages.Select(p => p.SocietyId).Distinct().Contains(s.Id))
                .ToDictionary(s => s.Id, s => s.Name);

        private Dictionary<Guid, DateTime> LoadDeliveredAt(List<Package> packages)
        {
            var packageIds = packages.Select(p => p.Id).ToList();
            if (packageIds.Count == 0)
                return new Dictionary<Guid, DateTime>();

            return _deliveries.GetAll()
                .Where(d => packageIds.Contains(d.PackageId) && d.Status.Value == "Completed")
                .AsEnumerable()
                .GroupBy(d => d.PackageId)
                .ToDictionary(g => g.Key, g => g.Max(d => d.DeliveredAt));
        }

        private Dictionary<Guid, List<PackageCustodyHop>> LoadHops(List<Package> packages)
        {
            var packageIds = packages.Select(p => p.Id).ToList();
            if (packageIds.Count == 0)
                return new Dictionary<Guid, List<PackageCustodyHop>>();

            return _custodyHops.GetAll()
                .Where(h => packageIds.Contains(h.PackageId))
                .AsEnumerable()
                .GroupBy(h => h.PackageId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(h => h.OccurredAtUtc).ThenBy(h => h.Id).ToList());
        }

        private static void WritePackageEvents(
            StringBuilder sb,
            Package package,
            string? societyName,
            DateTime? deliveredUtc,
            List<PackageCustodyHop> hops)
        {
            if (hops.Count > 0)
            {
                foreach (var hop in hops)
                {
                    AppendRow(
                        sb,
                        package,
                        societyName,
                        deliveredUtc,
                        hop.OccurredAtUtc,
                        hop.Kind,
                        PackageCustodyActivityFormatter.Format(hop, package.PkgNumber),
                        hop.FromZone,
                        hop.ToZone,
                        hop.LocationLabel,
                        hop.StaffUsername);
                }

                return;
            }

            // Legacy packages without custody hops — synthesize receive / shelve / deliver.
            AppendRow(
                sb,
                package,
                societyName,
                deliveredUtc,
                package.RecievedAt,
                PackageCustodyHopKinds.ReceivedAtHub,
                $"{package.PkgNumber} received at {societyName ?? PackageCustodyHopKinds.DefaultHubZone}",
                null,
                PackageCustodyHopKinds.DefaultHubZone,
                null,
                null);

            if (package.ShelfAssignedAtUtc is DateTime shelfAt)
            {
                AppendRow(
                    sb,
                    package,
                    societyName,
                    deliveredUtc,
                    shelfAt,
                    PackageCustodyHopKinds.Shelved,
                    $"{package.PkgNumber} shelved",
                    PackageCustodyHopKinds.DefaultHubZone,
                    package.TowerWing,
                    null,
                    null);
            }

            if (deliveredUtc is DateTime deliveredAtUtc)
            {
                AppendRow(
                    sb,
                    package,
                    societyName,
                    deliveredUtc,
                    deliveredAtUtc,
                    PackageCustodyHopKinds.Delivered,
                    $"{package.PkgNumber} delivered",
                    null,
                    null,
                    package.FlatNumber.Value,
                    null);
            }
        }

        private static void AppendRow(
            StringBuilder sb,
            Package package,
            string? societyName,
            DateTime? deliveredUtc,
            DateTime eventAt,
            string kind,
            string text,
            string? fromZone,
            string? toZone,
            string? locationLabel,
            string? staffUsername)
        {
            sb.Append(Csv(package.PkgNumber)).Append(',');
            sb.Append(Csv(package.SphTrackingNumber?.Value)).Append(',');
            sb.Append(Csv(package.CourierAwb)).Append(',');
            sb.Append(Csv(package.RecipientName)).Append(',');
            sb.Append(Csv(package.CourierPartner)).Append(',');
            sb.Append(Csv(package.Status.ToString())).Append(',');
            sb.Append(Csv(package.RecievedAt.ToString("O", CultureInfo.InvariantCulture))).Append(',');
            sb.Append(Csv(deliveredUtc?.ToString("O", CultureInfo.InvariantCulture))).Append(',');
            sb.Append(Csv(package.TowerWing)).Append(',');
            sb.Append(Csv(package.FlatNumber.Value)).Append(',');
            sb.Append(Csv(societyName)).Append(',');
            sb.Append(Csv(eventAt.ToString("O", CultureInfo.InvariantCulture))).Append(',');
            sb.Append(Csv(kind)).Append(',');
            sb.Append(Csv(text)).Append(',');
            sb.Append(Csv(fromZone)).Append(',');
            sb.Append(Csv(toZone)).Append(',');
            sb.Append(Csv(locationLabel)).Append(',');
            sb.Append(Csv(staffUsername));
            sb.AppendLine();
        }

        private static string Csv(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var needsQuotes =
                value.Contains('"', StringComparison.Ordinal) ||
                value.Contains(',', StringComparison.Ordinal) ||
                value.Contains('\n', StringComparison.Ordinal) ||
                value.Contains('\r', StringComparison.Ordinal);
            return needsQuotes
                ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
                : value;
        }
    }
}
