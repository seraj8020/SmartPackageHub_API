using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Services;
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

        public GetRecentActivityQueryHandler(
            IMapper mapper,
            IRepository<Society> societies,
            IRepository<Package> packages,
            IRepository<Delivery> deliveries) : base(mapper)
        {
            _societies = societies;
            _packages = packages;
            _deliveries = deliveries;
        }

        protected override Task<IReadOnlyList<RecentActivityItemDto>> HandleAsync(GetRecentActivityQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var take = Math.Clamp(request.Take, 1, 100);

            var societyNames = _societies.GetAll().AsEnumerable()
                .Where(s => AnalyticsScope.Includes(request.SocietyId, s.Id))
                .ToDictionary(s => s.Id, s => s.Name);

            var packages = _packages.GetAll().AsEnumerable()
                .Where(p => AnalyticsScope.Includes(request.SocietyId, p.SocietyId))
                .ToList();
            var packageById = packages.ToDictionary(p => p.Id);

            var items = new List<RecentActivityItemDto>();

            foreach (var package in packages)
            {
                societyNames.TryGetValue(package.SocietyId, out var societyName);
                items.Add(new RecentActivityItemDto(
                    package.RecievedAt,
                    "intake",
                    $"{package.PkgNumber} received at {(societyName ?? "hub")}",
                    package.SocietyId,
                    societyName,
                    package.Id));

                if (package.Status is PackageStatus.Shelved or PackageStatus.Dispatched or PackageStatus.Delivered
                    && package.ShelfId is not null)
                {
                    items.Add(new RecentActivityItemDto(
                        package.RecievedAt.AddMinutes(5),
                        "shelf",
                        $"{package.PkgNumber} assigned to shelf",
                        package.SocietyId,
                        societyName,
                        package.Id));
                }
            }

            foreach (var delivery in _deliveries.GetAll().AsEnumerable()
                         .Where(d => AnalyticsScope.Includes(request.SocietyId, d.SocietyId)))
            {
                societyNames.TryGetValue(delivery.SocietyId, out var societyName);
                packageById.TryGetValue(delivery.PackageId, out var pkg);
                var pkgNumber = pkg?.PkgNumber ?? "package";
                var viaOtp = delivery.OtpLogId is not null ? " via OTP" : string.Empty;
                items.Add(new RecentActivityItemDto(
                    delivery.DeliveredAt,
                    "pickup",
                    $"{pkgNumber} collected{viaOtp} ({delivery.Method})",
                    delivery.SocietyId,
                    societyName,
                    delivery.PackageId));
            }

            var result = items
                .OrderByDescending(i => i.OccurredAtUtc)
                .Take(take)
                .ToList();

            return Task.FromResult<IReadOnlyList<RecentActivityItemDto>>(result);
        }
    }
}
