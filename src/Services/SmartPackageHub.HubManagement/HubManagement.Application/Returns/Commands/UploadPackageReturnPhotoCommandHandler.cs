using System.Security.Cryptography;
using HubManagement.Application.Returns.DTOs;
using HubManagement.Application.Returns.Services;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;

namespace HubManagement.Application.Returns.Commands
{
    public sealed class UploadPackageReturnPhotoCommandHandler
        : CreateCommandHandler<UploadPackageReturnPhotoCommand, PackageReturnCommandResult>
    {
        private static readonly Action<ILogger, Guid, int, Exception?> LogPhotoSaved =
            LoggerMessage.Define<Guid, int>(
                LogLevel.Information,
                new EventId(1, nameof(UploadPackageReturnPhotoCommandHandler)),
                "Saved return photo for return {ReturnId} sortOrder {SortOrder}");

        private readonly IRepository<PackageReturn> _returns;
        private readonly IRepository<PackageReturnImage> _images;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Resident> _residents;
        private readonly IParcelPhotoStore _photos;
        private readonly ILogger<UploadPackageReturnPhotoCommandHandler> _logger;

        public UploadPackageReturnPhotoCommandHandler(
            IRepository<PackageReturn> returns,
            IRepository<PackageReturnImage> images,
            IRepository<Package> packages,
            IRepository<Resident> residents,
            IParcelPhotoStore photos,
            ILogger<UploadPackageReturnPhotoCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _returns = returns;
            _images = images;
            _packages = packages;
            _residents = residents;
            _photos = photos;
            _logger = logger;
        }

        public override async Task<PackageReturnCommandResult> Handle(
            UploadPackageReturnPhotoCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var payload = request.Request ?? throw new ArgumentNullException(nameof(request));

            var packageReturn = await _returns.GetByIdAsync(request.ReturnId).ConfigureAwait(false);
            if (packageReturn is null)
                return PackageReturnCommandResult.NotFound("Return was not found.");

            if (!packageReturn.IsOpen)
                return PackageReturnCommandResult.Fail(
                    "Photos can only be added while the return is open.",
                    conflict: true);

            if (payload.SortOrder < 0 || payload.SortOrder > 2)
                return PackageReturnCommandResult.Fail("Photo sort order must be 0–2.");

            var existing = _images.FetchMulti(i => i.ReturnId == packageReturn.Id, noTracking: true).ToList();
            if (existing.Count >= PackageReturn.MaxPhotos)
                return PackageReturnCommandResult.Fail(
                    $"A return supports at most {PackageReturn.MaxPhotos} photos.",
                    conflict: true);

            if (existing.Any(i => i.SortOrder == payload.SortOrder))
                return PackageReturnCommandResult.Fail(
                    $"A photo already exists for sort order {payload.SortOrder}.",
                    conflict: true);

            if (string.IsNullOrWhiteSpace(payload.ImageBase64))
                return PackageReturnCommandResult.Fail("Image is required.");

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(StripDataUrl(payload.ImageBase64));
            }
            catch (FormatException)
            {
                return PackageReturnCommandResult.Fail("Return photo was not valid base64.");
            }

            if (bytes.Length == 0 || bytes.Length > 5_000_000)
                return PackageReturnCommandResult.Fail("Return photo must be under 5 MB.");

            var storageKey = await _photos.SaveReturnAsync(
                packageReturn.Id,
                payload.SortOrder,
                bytes,
                cancellationToken).ConfigureAwait(false);

            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            PackageReturnImage image;
            try
            {
                image = PackageReturnImage.Create(
                    packageReturn.Id,
                    storageKey,
                    hash,
                    "image/jpeg",
                    bytes.Length,
                    payload.SortOrder,
                    DateTime.UtcNow);
            }
            catch (ArgumentException ex)
            {
                return PackageReturnCommandResult.Fail(ex.Message);
            }

            _images.Insert(image);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogPhotoSaved(_logger, packageReturn.Id, payload.SortOrder, null);

            var package = await _packages.GetByIdAsync(packageReturn.PackageId).ConfigureAwait(false);
            var resident = await _residents.GetByIdAsync(packageReturn.ResidentId).ConfigureAwait(false);
            var photos = existing.Append(image).ToList();

            return PackageReturnCommandResult.Ok(
                PackageReturnMapper.ToDto(packageReturn, photos, package, resident),
                "Return photo saved.");
        }

        private static string StripDataUrl(string value)
        {
            var trimmed = value.Trim();
            var marker = "base64,";
            var index = trimmed.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? trimmed[(index + marker.Length)..] : trimmed;
        }
    }
}
