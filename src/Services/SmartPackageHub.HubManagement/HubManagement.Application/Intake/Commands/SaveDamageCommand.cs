using HubManagement.Application.Intake.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Intake.Commands
{
    public sealed record SaveDamageCommand(Guid IntakeId, DamageIntakeRequest Request) : IUpdateCommand<DamageSaveResult>;

    public sealed class SaveDamageCommandHandler : UpdateCommandHandler<SaveDamageCommand, DamageSaveResult>
    {
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

        public override async Task<DamageSaveResult> Handle(SaveDamageCommand command, CancellationToken cancellationToken)
        {
            var intake = await _intakes.GetByIdAsync(command.IntakeId);
            if (intake == null)
                return new DamageSaveResult(false, "Intake not found");

            if (!string.IsNullOrWhiteSpace(command.Request.ImageBase64))
            {
                byte[] bytes;
                try
                {
                    bytes = Convert.FromBase64String(command.Request.ImageBase64);
                }
                catch (FormatException)
                {
                    return new DamageSaveResult(false, "Damage photo was not valid base64");
                }

                if (bytes.Length > 1_500_000)
                    return new DamageSaveResult(false, "Damage photo is too large");

                var key = await _photos.SaveDamageAsync(command.IntakeId, bytes, cancellationToken);
                intake.DamageImagePath = key;
                var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
                var image = PackageImage.Create(intake.PackageId, intake.Id, "Damage", key, hash, DateTime.UtcNow);
                image.ContentType = "image/jpeg";
                image.ByteSize = bytes.Length;
                _images.Insert(image);
            }

            intake.IsDamaged = true;
            if (!string.IsNullOrWhiteSpace(command.Request.Note))
                intake.DamageNote = IntakeText.Clean(command.Request.Note, 240);

            await UnitOfWork!.CommitAsync(cancellationToken);
            _logger.LogInformation("Saved damage details for intake {IntakeId}", intake.Id);
            return new DamageSaveResult(true, null);
        }
    }
}
