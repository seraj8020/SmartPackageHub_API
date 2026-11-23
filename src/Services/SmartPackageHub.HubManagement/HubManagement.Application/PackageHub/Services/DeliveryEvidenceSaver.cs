using System.Security.Cryptography;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;

namespace HubManagement.Application.PackageHub.Services
{
    internal static class DeliveryEvidenceSaver
    {
        private const int MaxBytes = 5_000_000;

        public static string? ValidateBase64(string? base64, string label)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return null;

            try
            {
                var bytes = Convert.FromBase64String(StripDataUrl(base64));
                if (bytes.Length == 0 || bytes.Length > MaxBytes)
                    return $"{label} evidence must be under 5 MB.";
                return null;
            }
            catch (FormatException)
            {
                return $"{label} evidence was not valid base64.";
            }
        }

        public static async Task<string?> TrySaveAsync(
            IParcelPhotoStore photos,
            IRepository<PackageDeliveryEvidence> evidence,
            Guid deliveryId,
            Guid packageId,
            string kind,
            string? base64,
            string? signedByName,
            DateTime capturedAtUtc,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return null;

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(StripDataUrl(base64));
            }
            catch (FormatException)
            {
                return $"{kind} evidence was not valid base64.";
            }

            if (bytes.Length == 0 || bytes.Length > MaxBytes)
                return $"{kind} evidence must be under 5 MB.";

            var isSignature = string.Equals(kind, DeliveryEvidenceKind.Signature, StringComparison.Ordinal);
            string contentType;
            if (isSignature)
                contentType = bytes[0] == (byte)'<' ? "image/svg+xml" : "image/png";
            else
                contentType = "image/jpeg";

            var storageKey = await photos.SaveHandoverAsync(
                    deliveryId,
                    kind,
                    bytes,
                    contentType,
                    cancellationToken)
                .ConfigureAwait(false);

            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            PackageDeliveryEvidence row;
            try
            {
                row = PackageDeliveryEvidence.Create(
                    deliveryId,
                    packageId,
                    kind,
                    storageKey,
                    hash,
                    contentType,
                    bytes.Length,
                    capturedAtUtc,
                    isSignature ? signedByName : null);
            }
            catch (ArgumentException ex)
            {
                return ex.Message;
            }

            evidence.Insert(row);
            return null;
        }

        public static string StripDataUrl(string value)
        {
            var trimmed = value.Trim();
            var comma = trimmed.IndexOf(',', StringComparison.Ordinal);
            if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
                return trimmed[(comma + 1)..];
            return trimmed;
        }
    }
}
