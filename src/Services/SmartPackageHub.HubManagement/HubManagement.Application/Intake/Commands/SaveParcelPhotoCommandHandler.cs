using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Common.Services.Storage.Abstractions;
using SmartPackageHub.Common.Services.Storage.DTOs;
using SmartPackageHub.Common.Services.Storage.Services;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using HubManagement.Application.Intake.Services;

namespace HubManagement.Application.Intake.Commands
{
    public sealed class SaveParcelPhotoCommandHandler : CreateCommandHandler<SaveParcelPhotoCommand, PhotoUploadResult>
    {
        private static readonly Action<ILogger, string, bool, Exception?> LogSaveParcelPhoto =
            LoggerMessage.Define<string, bool>(
                LogLevel.Information,
                new EventId(1, nameof(SaveParcelPhotoCommandHandler)),
                "Stored parcel photo {ClientPhotoId}. Linked: {Linked}");
        private readonly IRepository<PackageImage> _images;
        private readonly IParcelPhotoStore _photos;
        private readonly ILogger<SaveParcelPhotoCommandHandler> _logger;

        public SaveParcelPhotoCommandHandler(
            IRepository<PackageImage> images,
            IParcelPhotoStore photos,
            ILogger<SaveParcelPhotoCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _images = images;
            _photos = photos;
            _logger = logger;
        }

        public override async Task<PhotoUploadResult> Handle(SaveParcelPhotoCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var id = request.ClientPhotoId?.Trim() ?? string.Empty;
            if (!Guid.TryParse(id, out _))
                return new PhotoUploadResult(false, "Photo id is not valid", null);
            if (request.Bytes.Length == 0 || request.Bytes.Length > 1_500_000)
                return new PhotoUploadResult(false, "Parcel photo must be under 1.5 MB", null);

            var saved = await _photos.SaveAsync(id, request.Bytes, request.ContentType, cancellationToken).ConfigureAwait(false);
            var linked = false;
            var inserted = false;
            for (var attempt = 0; attempt < 8 && !linked; attempt++)
            {
                var result = await IntakePhotoLinker.LinkAsync(_photos, _images, id, null, null, cancellationToken).ConfigureAwait(false);
                linked = result.Linked;
                inserted |= result.Inserted;
                if (!linked)
                    await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }

            if (inserted)
                await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogSaveParcelPhoto(_logger, id, linked, null);
            return new PhotoUploadResult(true, null, new PhotoUploadResponse
            {
                ClientPhotoId = id,
                StorageKey = saved.StorageKey,
                Linked = linked
            });
        }
    }
}
