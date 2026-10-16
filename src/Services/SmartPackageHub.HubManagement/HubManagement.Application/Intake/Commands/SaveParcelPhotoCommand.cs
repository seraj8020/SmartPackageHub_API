using HubManagement.Application.Intake.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Intake.Commands
{
    public sealed record SaveParcelPhotoCommand(string? ClientPhotoId, byte[] Bytes, string ContentType) : ICreateCommand<PhotoUploadResult>;

    public sealed class SaveParcelPhotoCommandHandler : CreateCommandHandler<SaveParcelPhotoCommand, PhotoUploadResult>
    {
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
            var id = request.ClientPhotoId?.Trim() ?? string.Empty;
            if (!Guid.TryParse(id, out _))
                return new PhotoUploadResult(false, "Photo id is not valid", null);
            if (request.Bytes.Length == 0 || request.Bytes.Length > 1_500_000)
                return new PhotoUploadResult(false, "Parcel photo must be under 1.5 MB", null);

            var saved = await _photos.SaveAsync(id, request.Bytes, request.ContentType, cancellationToken);
            var linked = false;
            var inserted = false;
            for (var attempt = 0; attempt < 8 && !linked; attempt++)
            {
                var result = await IntakePhotoLinker.LinkAsync(_photos, _images, id, null, null, cancellationToken);
                linked = result.Linked;
                inserted |= result.Inserted;
                if (!linked)
                    await Task.Delay(150, cancellationToken);
            }

            if (inserted)
                await UnitOfWork!.CommitAsync(cancellationToken);

            _logger.LogInformation("Stored parcel photo {ClientPhotoId}. Linked: {Linked}", id, linked);
            return new PhotoUploadResult(true, null, new PhotoUploadResponse
            {
                ClientPhotoId = id,
                StorageKey = saved.StorageKey,
                Linked = linked
            });
        }
    }

    
}
