using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Common.Services.Storage.Abstractions;
using SmartPackageHub.Common.Services.Storage.DTOs;
using SmartPackageHub.Common.Services.Storage.Services;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using HubManagement.Application.Intake.Helpers;
using HubManagement.Application.Intake.Services;

namespace HubManagement.Application.Intake.Commands
{
    public sealed class SaveDamageCommandHandler : UpdateCommandHandler<SaveDamageCommand, DamageSaveResult>
    {
        private static readonly Action<ILogger, Guid, string, Exception?> LogSaveDamageImageConfirmed =
            LoggerMessage.Define<Guid, string?>(
                LogLevel.Information,
                new EventId(1, nameof(SaveDamageCommandHandler)),
                "Saved damage details for intake {IntakeId} for Package {PackageId}");
        private readonly IRepository<PackageIntake> _intakes;
        private readonly IRepository<PackageImage> _images;
        private readonly IParcelPhotoStore _photos;
        private readonly ILogger<SaveDamageCommandHandler> _logger;

        public SaveDamageCommandHandler(
            IRepository<PackageIntake> intakes,
            IRepository<PackageImage> images,
            IParcelPhotoStore photos,
            ILogger<SaveDamageCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _intakes = intakes;
            _images = images;
            _photos = photos;
            _logger = logger;
        }

        public override async Task<DamageSaveResult> Handle(SaveDamageCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var intake = await _intakes.GetByIdAsync(request.IntakeId).ConfigureAwait(false);
            if (intake == null)
                return new DamageSaveResult(false, "Intake not found");

            if (!string.IsNullOrWhiteSpace(request.Request.ImageBase64))
            {
                byte[] bytes;
                try
                {
                    bytes = Convert.FromBase64String(StripDataUrl(request.Request.ImageBase64));
                }
                catch (FormatException)
                {
                    return new DamageSaveResult(false, "Damage photo was not valid base64");
                }

                if (bytes.Length > 1_500_000)
                    return new DamageSaveResult(false, "Damage photo is too large");

                var key = await _photos.SaveDamageAsync(request.IntakeId, bytes, cancellationToken).ConfigureAwait(false);
                intake.DamageImagePath = key;
                var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
                var image = PackageImage.Create(intake.PackageId, intake.Id, "Damage", key, hash, DateTime.UtcNow);
                image.ContentType = "image/jpeg";
                image.ByteSize = bytes.Length;
                _images.Insert(image);
            }

            intake.IsDamaged = true;
            if (!string.IsNullOrWhiteSpace(request.Request.Note))
                intake.DamageNote = IntakeText.Clean(request.Request.Note, 240);

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogSaveDamageImageConfirmed(_logger, intake.Id, intake.PackageId.ToString(), null);
            return new DamageSaveResult(true, null);
        }

        /// <summary>
        /// Mobile/web often send a data URL (data:image/jpeg;base64,...) — strip the prefix for Convert.FromBase64String.
        /// </summary>
        private static string StripDataUrl(string value)
        {
            var trimmed = value.Trim();
            var marker = "base64,";
            var index = trimmed.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? trimmed[(index + marker.Length)..] : trimmed;
        }
    }
}
