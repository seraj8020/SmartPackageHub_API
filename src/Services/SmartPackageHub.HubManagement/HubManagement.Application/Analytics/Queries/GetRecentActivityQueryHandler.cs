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

            var societyNames = _societies.GetAll()
                .Where(s => request.SocietyId == null || request.SocietyId == Guid.Empty || s.Id == request.SocietyId.Value)
                .ToDictionary(s => s.Id, s => s.Name);

            var packages = _packages.GetAll()
                .Where(p => request.SocietyId == null || request.SocietyId == Guid.Empty || p.SocietyId == request.SocietyId.Value)
                .OrderByDescending(p => p.RecievedAt)
                .ThenByDescending(p => p.Id)
                .Take(take)
                .ToList();
            var shelfPackages = _packages.GetAll()
                .Where(p => (request.SocietyId == null || request.SocietyId == Guid.Empty || p.SocietyId == request.SocietyId.Value) &&
                    p.ShelfAssignedAtUtc.HasValue)
                .OrderByDescending(p => p.ShelfAssignedAtUtc)
                .ThenByDescending(p => p.Id)
                .Take(take)
                .ToList();
            var deliveries = _deliveries.GetAll()
                .Where(d => (request.SocietyId == null || request.SocietyId == Guid.Empty || d.SocietyId == request.SocietyId.Value) &&
                    d.Status.Value == "Completed")
                .OrderByDescending(d => d.DeliveredAt)
                .ThenByDescending(d => d.Id)
                .Take(take)
                .ToList();
            var relatedPackageIds = shelfPackages.Select(p => p.Id)
                .Concat(deliveries.Select(d => d.PackageId))
                .Distinct()
                .ToList();
            var relatedPackagesById = relatedPackageIds.Count == 0
                ? new Dictionary<Guid, Package>()
                : _packages.GetAll()
                    .Where(p => relatedPackageIds.Contains(p.Id) &&
                        (request.SocietyId == null || request.SocietyId == Guid.Empty || p.SocietyId == request.SocietyId.Value))
                    .ToDictionary(p => p.Id);

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

            }

            foreach (var package in shelfPackages)
            {
                societyNames.TryGetValue(package.SocietyId, out var societyName);
                items.Add(new RecentActivityItemDto(
                    package.ShelfAssignedAtUtc!.Value,
                    "shelf",
                    $"{package.PkgNumber} assigned to shelf",
                    package.SocietyId,
                    societyName,
                    package.Id));
            }

            foreach (var society in _societies.GetAll()
                         .Where(s => (request.SocietyId == null || request.SocietyId == Guid.Empty || s.Id == request.SocietyId.Value) &&
                             s.OnboardedAtUtc.HasValue)
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

            foreach (var delivery in deliveries)
            {
                societyNames.TryGetValue(delivery.SocietyId, out var societyName);
                relatedPackagesById.TryGetValue(delivery.PackageId, out var pkg);
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
