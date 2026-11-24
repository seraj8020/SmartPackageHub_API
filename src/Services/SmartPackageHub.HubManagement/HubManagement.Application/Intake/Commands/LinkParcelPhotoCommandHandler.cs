using HubManagement.Application.Intake.DTOs;
using HubManagement.Application.Intake.Services;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;

namespace HubManagement.Application.Intake.Commands
{
    /// <summary>
    /// Links an already-uploaded arrival photo to intake/package without re-sending image bytes.
    /// Used after ConfirmIntake when PhotoStored is still false.
    /// </summary>
    public sealed class LinkParcelPhotoCommandHandler
        : CreateCommandHandler<LinkParcelPhotoCommand, PhotoUploadResult>
    {
        private readonly IRepository<PackageImage> _images;
        private readonly IParcelPhotoStore _photos;

        public LinkParcelPhotoCommandHandler(
            IRepository<PackageImage> images,
            IParcelPhotoStore photos,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _images = images;
            _photos = photos;
        }

        public override async Task<PhotoUploadResult> Handle(
            LinkParcelPhotoCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var body = request.Request ?? new LinkParcelPhotoRequest();
            var clientPhotoId = body.ClientPhotoId?.Trim() ?? string.Empty;
            if (!Guid.TryParse(clientPhotoId, out _))
                return new PhotoUploadResult(false, "Photo id is not valid", null);

            if (body.IntakeId is Guid intakeId && body.PackageId is Guid packageId)
            {
                await _photos.WriteLinkAsync(clientPhotoId, intakeId, packageId, cancellationToken)
                    .ConfigureAwait(false);
            }

            var linked = await IntakePhotoLinker.LinkAsync(
                    _photos,
                    _images,
                    clientPhotoId,
                    body.IntakeId,
                    body.PackageId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (linked.Inserted)
                await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new PhotoUploadResult(
                linked.Linked,
                linked.Linked ? null : "Arrival photo was not found or could not be linked",
                linked.Linked
                    ? new PhotoUploadResponse
                    {
                        ClientPhotoId = clientPhotoId,
                        StorageKey = string.Empty,
                        Linked = true,
                    }
                    : null);
        }
    }
}