using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;

namespace HubManagement.Application.HubResidents.Services
{
    internal static class ResidentReturnRequestBuilder
    {
        public static PackageReturn? FindForPackage(
            IRepository<PackageReturn> returns,
            Guid packageId,
            Guid residentId,
            bool preferOpen = true)
        {
            var rows = returns.FetchMulti(
                    r => r.PackageId == packageId && r.ResidentId == residentId,
                    noTracking: true)
                .AsEnumerable()
                .OrderByDescending(r => r.RequestedAtUtc)
                .ToList();

            if (rows.Count == 0)
                return null;

            if (preferOpen)
            {
                var open = rows.FirstOrDefault(r =>
                    r.Status is PackageReturnStatus.ReturnRequested
                        or PackageReturnStatus.ReturnReceived
                        or PackageReturnStatus.ReturnShelved);
                if (open is not null)
                    return open;
            }

            return rows[0];
        }

        public static async Task<ResidentReturnRequestDto> ToDtoAsync(
            PackageReturn packageReturn,
            IReadOnlyList<PackageReturnImage> photos,
            IParcelPhotoStore photoStore,
            Guid residentId,
            CancellationToken cancellationToken = default)
        {
            var photoDtos = new List<ResidentReturnPhotoDto>(photos.Count);
            foreach (var photo in photos.OrderBy(p => p.SortOrder))
            {
                var (url, expires) = await ResidentReturnPhotoUrls.ResolveAsync(
                        photoStore,
                        residentId,
                        packageReturn.PackageId,
                        photo.Id,
                        photo.StorageKey,
                        cancellationToken)
                    .ConfigureAwait(false);

                photoDtos.Add(new ResidentReturnPhotoDto
                {
                    Id = photo.Id,
                    SortOrder = photo.SortOrder,
                    CapturedAtUtc = photo.CapturedAtUtc,
                    PhotoUrl = url,
                    PhotoExpiresAtUtc = expires,
                });
            }

            return new ResidentReturnRequestDto
            {
                Id = packageReturn.Id,
                PackageId = packageReturn.PackageId,
                Status = packageReturn.Status.ToString(),
                Reason = packageReturn.Reason.ToString(),
                Note = packageReturn.Note,
                RequestedAtUtc = packageReturn.RequestedAtUtc,
                Photos = photoDtos,
            };
        }
    }
}
