using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Common.Services.Storage.Abstractions;
using SmartPackageHub.Common.Services.Storage.DTOs;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Intake.Commands
{
    public sealed class SaveParcelPhotoCommandHandler : CreateCommandHandler<SaveParcelPhotoCommand, PhotoUploadResult>
    {
        private static readonly Action<ILogger, string, bool, Exception?> LogSaveParcelPhoto =
            LoggerMessage.Define<string, bool>(
                LogLevel.Information,
                new EventId(1, nameof(SaveParcelPhotoCommandHandler)),
                "Stored parcel photo {ClientPhotoId}. Linked: {Linked}");
        private readonly IParcelPhotoStore _photos;
        private readonly ILogger<SaveParcelPhotoCommandHandler> _logger;

        public SaveParcelPhotoCommandHandler(
            IParcelPhotoStore photos,
            ILogger<SaveParcelPhotoCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
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

            // Save blob only. ConfirmIntake writes the intake↔photo link after the package exists.
            // The old 8×150ms poll waited for a link that Confirm had not created yet (~1.2s wasted).
            var saved = await _photos.SaveAsync(id, request.Bytes, request.ContentType, cancellationToken)
                .ConfigureAwait(false);
            LogSaveParcelPhoto(_logger, id, false, null);
            return new PhotoUploadResult(true, null, new PhotoUploadResponse
            {
                ClientPhotoId = id,
                StorageKey = saved.StorageKey,
                Linked = false
            });
        }
    }
}
