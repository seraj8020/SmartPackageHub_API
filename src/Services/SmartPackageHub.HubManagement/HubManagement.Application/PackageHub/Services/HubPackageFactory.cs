using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.PackageHub.Services
{
    internal static class HubPackageFactory
    {
        public static string? ValidateTracking(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Tracking number is required";

            var trimmed = value.Trim();
            if (trimmed.Length < 8)
                return "Tracking number must be at least 8 characters";
            if (trimmed.Length > max)
                return "Tracking number is too long";

            return null;
        }

        public static string NextNumber(IRepository<Package> packages)
        {
            var next = packages.GetAll().Count() + 1;
            return $"PKG-{next:D5}";
        }

        public static Package? FindOpen(IRepository<Package> packages, string awb) =>
            packages.FetchMulti(package =>
                    package.Status != PackageStatus.Delivered &&
                    (package.CourierAwb == awb || package.SphTrackingNumber.Value == awb))
                .FirstOrDefault();

        public static Package Create(
            Guid societyId,
            Resident? resident,
            Guid? shelfId,
            Guid staffUserId,
            string trackingNumber,
            string pkgNumber,
            string? courier,
            string? description,
            string? recipientName,
            string? tower,
            string? flat,
            bool isColdStorage,
            string? category = null,
            double? weight = null,
            double? length = null,
            double? width = null,
            double? height = null,
            bool isCod = false,
            decimal? codAmount = null)
        {
            var name = Limit(First(recipientName, resident?.FullName) ?? "Unassigned", 150);
            var wing = Limit(First(tower, resident?.TowerWing) ?? "NA", 100);
            var unit = Limit(First(flat, resident?.Unit?.Value) ?? "NA", 50);
            var partner = Limit(string.IsNullOrWhiteSpace(courier) ? "Courier" : courier.Trim(), 100);
            var resolvedCategory = !string.IsNullOrWhiteSpace(category)
                ? Limit(category.Trim(), 100)
                : !string.IsNullOrWhiteSpace(description)
                    ? Limit(description.Trim(), 100)
                    : "Miscellaneous";
            var dimensions = PackageDimensions.Create(
                PositiveOrDefault(weight, 1),
                PositiveOrDefault(height, 1),
                PositiveOrDefault(width, 1),
                PositiveOrDefault(length, 1));
            var resolvedCodAmount = isCod ? codAmount : null;
            return Package.Create(
                societyId,
                resident?.Id,
                shelfId,
                staffUserId,
                trackingNumber,
                dimensions,
                partner,
                trackingNumber,
                name,
                wing,
                unit,
                PackageStatus.Received,
                DateTime.UtcNow,
                isColdStorage: isColdStorage,
                isCod: isCod,
                codAmount: resolvedCodAmount,
                pkgNumber: pkgNumber,
                category: resolvedCategory);
        }

        private static double PositiveOrDefault(double? value, double fallback) =>
            value is > 0 ? value.Value : fallback;

        private static string Limit(string value, int max) =>
            value.Length <= max ? value : value[..max];

        private static string? First(params string?[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }
}
