using System.Globalization;
using System.Security.Cryptography;
using HubManagement.Application.Intake.DTOs;
using HubManagement.Application.PackageHub;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Intake.Commands
{
    public sealed class ConfirmIntakeCommandHandler : CreateCommandHandler<ConfirmIntakeCommand, IntakeStatusResult<ConfirmIntakeResponse>>
    {
        private static readonly TimeSpan PickupOtpLifetime = TimeSpan.FromDays(7);

        private static readonly Action<ILogger, Guid, string?, Exception?> LogIntakeConfirmed =
            LoggerMessage.Define<Guid, string?>(
                LogLevel.Information,
                new EventId(1, nameof(ConfirmIntakeCommandHandler)),
                "Confirmed intake {IntakeId} for AWB {Awb}");

        private readonly IRepository<Package> _packages;
        private readonly IRepository<PackageIntake> _intakes;
        private readonly IRepository<PackageImage> _images;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<HubManagement.Core.Hubs.Entities.User> _users;
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<CourierAgent> _agents;
        private readonly IRepository<OtpLog> _otpLogs;
        private readonly IRepository<Notification> _notifications;
        private readonly IParcelPhotoStore _photos;
        private readonly IntakeNotificationChannel _queue;
        private readonly ILogger<ConfirmIntakeCommandHandler> _logger;

        public ConfirmIntakeCommandHandler(
            IRepository<Package> packages,
            IRepository<PackageIntake> intakes,
            IRepository<PackageImage> images,
            IRepository<Resident> residents,
            IRepository<Society> societies,
            IRepository<HubManagement.Core.Hubs.Entities.User> users,
            IRepository<Shelf> shelves,
            IRepository<CourierAgent> agents,
            IRepository<OtpLog> otpLogs,
            IRepository<Notification> notifications,
            IParcelPhotoStore photos,
            IntakeNotificationChannel queue,
            ILogger<ConfirmIntakeCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _intakes = intakes;
            _images = images;
            _residents = residents;
            _societies = societies;
            _users = users;
            _shelves = shelves;
            _agents = agents;
            _otpLogs = otpLogs;
            _notifications = notifications;
            _photos = photos;
            _queue = queue;
            _logger = logger;
        }

        public override async Task<IntakeStatusResult<ConfirmIntakeResponse>> Handle(ConfirmIntakeCommand command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            var request = command.Request;
            var clientRequestId = IntakeText.Clean(request.ClientRequestId, 80);
            var duplicateResult = await CheckDuplicateAsync(request, clientRequestId, cancellationToken).ConfigureAwait(false);
            if (duplicateResult != null)
            {
                return duplicateResult;
            }

            var (awb, courier, name, tower, flat, trackingError) = await ValidateAndCleanTrackingAsync(request).ConfigureAwait(false);
            if (trackingError != null)
            {
                return trackingError;
            }

            var (resident, matchedSocietyId, ambiguous, residentError) = await ResolveResidentAndSocietyAsync(request, tower, flat).ConfigureAwait(false);
            if (residentError != null)
            {
                return residentError;
            }

            var (placed, resolvedResident, placementError) = await ResolvePlacementAsync(request, resident, matchedSocietyId).ConfigureAwait(false);
            if (placementError != null)
            {
                return placementError;
            }

            var shelf = ResolveShelf(request, placed!.SocietyId);
            var matchStatus = DetermineMatchStatus(request.MatchStatus, resolvedResident, ambiguous);

            return await PersistAndConfirmAsync(
                request,
                placed,
                resolvedResident,
                shelf,
                awb!,
                courier,
                name,
                tower,
                flat,
                matchStatus,
                clientRequestId,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<(string? Awb, string? Courier, string? Name, string? Tower, string? Flat, IntakeStatusResult<ConfirmIntakeResponse>? Error)> ValidateAndCleanTrackingAsync(ConfirmIntakeRequest request)
        {
            var (awb, courier, name, tower, flat, trackingError) = ParseAndValidateTracking(request);
            if (trackingError != null)
            {
                var errorMessage = string.Equals(trackingError, "Tracking number is required", StringComparison.Ordinal)
                    ? "Tracking number is required"
                    : trackingError;
                return (null, null, null, null, null, new IntakeStatusResult<ConfirmIntakeResponse>(400, errorMessage, null));
            }

            awb = awb!.Trim();
            IntakeText.SplitUnit(ref tower, ref flat);
            tower = LabelTextParser.CleanTower(tower);
            flat = LabelTextParser.CleanFlat(flat);

            var conflictResult = await CheckAwbConflictAsync(awb).ConfigureAwait(false);
            if (conflictResult != null)
            {
                return (null, null, null, null, null, conflictResult);
            }

            return (awb, courier, name, tower, flat, null);
        }

        private async Task<(Placement? Placed, Resident? Resident, IntakeStatusResult<ConfirmIntakeResponse>? Error)> ResolvePlacementAsync(
            ConfirmIntakeRequest request,
            Resident? resident,
            Guid? matchedSocietyId)
        {
            var placed = await PackagePlacement.ResolveAsync(
                _residents,
                _societies,
                _users,
                resident?.Id,
                matchedSocietyId ?? resident?.SocietyId,
                request.StaffUserId,
                requireResidentWhenSocietyMissing: false).ConfigureAwait(false);

            if (!placed.Succeeded)
            {
                return (null, null, new IntakeStatusResult<ConfirmIntakeResponse>(placed.Found ? 400 : 404, placed.Message, null));
            }

            return (placed, placed.Resident ?? resident, null);
        }

        private async Task<IntakeStatusResult<ConfirmIntakeResponse>?> CheckDuplicateAsync(
            ConfirmIntakeRequest request,
            string? clientRequestId,
            CancellationToken cancellationToken)
        {
            if (clientRequestId == null)
            {
                return null;
            }

            var prior = _intakes.FetchMulti(intake => intake.ClientRequestId == clientRequestId).FirstOrDefault();
            if (prior == null)
            {
                return null;
            }

            var priorPackage = await _packages.GetByIdAsync(prior.PackageId).ConfigureAwait(false);
            if (priorPackage == null)
            {
                return null;
            }

            await LinkPhotoAsync(IntakeText.Clean(request.ClientPhotoId, 80), prior.Id, priorPackage.Id, cancellationToken).ConfigureAwait(false);
            return new IntakeStatusResult<ConfirmIntakeResponse>(200, null, await MapAsync(prior, priorPackage, null).ConfigureAwait(false));
        }

        private static (string? awb, string? courier, string? name, string? tower, string? flat, string? trackingError) ParseAndValidateTracking(ConfirmIntakeRequest request)
        {
            var tower = request.Tower;
            var flat = request.FlatNumber;
            var name = request.RecipientName;
            var courier = request.Courier;
            var awb = IntakeText.First(request.Awb, request.Barcode);

            if (string.IsNullOrWhiteSpace(awb) && !string.IsNullOrWhiteSpace(request.RawOcrText))
            {
                var parsed = LabelTextParser.Parse(request.RawOcrText);
                awb = IntakeText.First(awb, parsed.Awb);
                courier ??= parsed.Courier;
                name ??= parsed.RecipientName;
                tower ??= parsed.Tower;
                flat ??= parsed.FlatNumber;
            }

            var trackingError = HubPackageFactory.ValidateTracking(awb, 40);
            return (awb, courier, name, tower, flat, trackingError);
        }

        private async Task<IntakeStatusResult<ConfirmIntakeResponse>?> CheckAwbConflictAsync(string awb)
        {
            var open = HubPackageFactory.FindOpen(_packages, awb);
            if (open == null)
            {
                return null;
            }

            var existingShelf = open.ShelfId is Guid existingShelfId
                ? (await _shelves.GetByIdAsync(existingShelfId).ConfigureAwait(false))?.Code
                : null;

            return new IntakeStatusResult<ConfirmIntakeResponse>(
                409,
                $"This AWB is already at the hub on shelf {existingShelf ?? "unassigned"} ({open.PkgNumber}).",
                null);
        }

        private async Task<(Resident? resident, Guid? matchedSocietyId, bool ambiguous, IntakeStatusResult<ConfirmIntakeResponse>? error)> ResolveResidentAndSocietyAsync(
            ConfirmIntakeRequest request,
            string? tower,
            string? flat)
        {
            Resident? resident = null;
            if (request.ResidentId is Guid residentId && residentId != Guid.Empty)
            {
                resident = await _residents.GetByIdAsync(residentId).ConfigureAwait(false);
                if (resident == null)
                {
                    return (null, null, false, new IntakeStatusResult<ConfirmIntakeResponse>(404, "Resident was not found.", null));
                }
            }

            var matchedSocietyId = request.SocietyId;
            var ambiguous = false;
            if (resident == null && tower != null && flat != null)
            {
                var hits = ResidentMatcher.ByTowerAndFlat(_residents.GetAll().ToList(), tower, flat);
                if (hits.Count == 1)
                {
                    resident = hits[0];
                }
                else if (hits.Count > 1)
                {
                    ambiguous = true;
                    var societies = hits.Select(hit => hit.SocietyId).Distinct().ToList();
                    if (matchedSocietyId == null && societies.Count == 1)
                    {
                        matchedSocietyId = societies[0];
                    }
                }
            }

            return (resident, matchedSocietyId, ambiguous, null);
        }

        private Shelf? ResolveShelf(ConfirmIntakeRequest request, Guid societyId)
        {
            var shelves = _shelves.GetAll().Where(shelf => shelf.SocietyId == societyId).ToList();
            var requestedShelf = IntakeText.Clean(request.ShelfCode, 50);
            var shelf = requestedShelf == null
                ? null
                : shelves.FirstOrDefault(item => string.Equals(item.Code, requestedShelf, StringComparison.OrdinalIgnoreCase));

            return shelf ?? ShelfSuggester.Suggest(shelves, _packages.GetAll().Where(package => package.SocietyId == societyId).ToList(), societyId);
        }

        private static string DetermineMatchStatus(string? requestedStatus, Resident? resident, bool ambiguous)
        {
            var matchStatus = requestedStatus?.Trim().ToUpperInvariant();
            if (matchStatus is "matched" or "ambiguous" or "manual" or "missing")
            {
                return matchStatus;
            }

            if (resident == null)
            {
                return ambiguous ? "ambiguous" : "missing";
            }

            return "matched";
        }

        private async Task<IntakeStatusResult<ConfirmIntakeResponse>> PersistAndConfirmAsync(
            ConfirmIntakeRequest request,
            Placement placed,
            Resident? resident,
            Shelf? shelf,
            string awb,
            string? courier,
            string? name,
            string? tower,
            string? flat,
            string matchStatus,
            string? clientRequestId,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var pkgNumber = $"PKG-{now:yyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
            var description = string.IsNullOrWhiteSpace(name) ? "Label intake" : $"Intake for {IntakeText.Clean(name, 80)}";
            var package = HubPackageFactory.Create(
                placed.SocietyId,
                resident,
                shelf?.Id,
                placed.StaffUserId,
                awb,
                pkgNumber,
                courier,
                description,
                name,
                tower ?? resident?.TowerWing,
                flat ?? resident?.Unit?.Value,
                shelf?.IsColdStorage ?? false);

            if (shelf != null)
            {
                package.ShelfPackage();
            }

            var agent = FindAgent(placed.SocietyId, courier);
            package.PackageIntake.RecordLabelScan(
                agent?.Id,
                now,
                matchStatus,
                IntakeText.Clean(request.RawOcrText, 4000),
                request.OcrElapsedMs,
                clientRequestId ?? string.Empty,
                IntakeText.Clean(request.Barcode, 40),
                request.IsDamaged,
                IntakeText.Clean(request.DamageNote, 240));

            string? otp = null;
            _packages.Insert(package);
            if (resident != null)
            {
                otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
                _otpLogs.Insert(OtpLog.Generate(resident.SocietyId, resident.Id, package.Id, otp, PickupOtpLifetime, "Pickup"));
            }

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            await LinkPhotoAsync(IntakeText.Clean(request.ClientPhotoId, 80), package.PackageIntake.Id, package.Id, cancellationToken).ConfigureAwait(false);
            _queue.Enqueue(package.PackageIntake.Id);
            LogIntakeConfirmed(_logger, package.PackageIntake.Id, awb, null);
            return new IntakeStatusResult<ConfirmIntakeResponse>(200, null, await MapAsync(package.PackageIntake, package, otp).ConfigureAwait(false));
        }

        private CourierAgent? FindAgent(Guid societyId, string? courier)
        {
            if (string.IsNullOrWhiteSpace(courier))
                return null;

            return _agents.GetAll().AsEnumerable().FirstOrDefault(agent =>
                agent.IsActive &&
                agent.SocietyId == societyId &&
                (courier.Contains(agent.CarrierName, StringComparison.OrdinalIgnoreCase) ||
                 agent.CarrierName.Contains(courier, StringComparison.OrdinalIgnoreCase)));
        }

        private async Task LinkPhotoAsync(string? clientPhotoId, Guid intakeId, Guid packageId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(clientPhotoId) || !Guid.TryParse(clientPhotoId, out _))
                return;

            await _photos.WriteLinkAsync(clientPhotoId, intakeId, packageId, cancellationToken).ConfigureAwait(false);
            var linked = await IntakePhotoLinker.LinkAsync(_photos, _images, clientPhotoId, intakeId, packageId, cancellationToken).ConfigureAwait(false);
            if (linked.Inserted)
                await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<ConfirmIntakeResponse> MapAsync(PackageIntake intake, Package package, string? knownOtp)
        {
            ResidentMatchDto? resident = null;
            if (package.ResidentId is Guid residentId)
            {
                var row = await _residents.GetByIdAsync(residentId).ConfigureAwait(false);
                if (row != null)
                    resident = ResidentMatcher.ToDto(row);
            }

            var shelfCode = package.ShelfId is Guid shelfId ? (await _shelves.GetByIdAsync(shelfId).ConfigureAwait(false))?.Code ?? string.Empty : string.Empty;
            var otp = knownOtp ?? _otpLogs.GetAll()
                .Where(log => log.PackageId == package.Id && log.Purpose == "Pickup")
                .AsEnumerable()
                .OrderByDescending(log => log.Expiry?.Value)
                .Select(log => log.Token?.Value)
                .FirstOrDefault();
            var notified = _notifications.GetAll().Any(notification =>
                notification.PackageId == package.Id && notification.Content.TemplateName == "IntakeReceived");

            return new ConfirmIntakeResponse
            {
                IntakeId = intake.Id,
                PackageId = package.Id,
                PkgNumber = package.PkgNumber,
                Awb = package.CourierAwb,
                Courier = package.CourierPartner,
                ShelfCode = shelfCode,
                PickupOtp = otp,
                PickupQrPayload = $"SPH|{package.PkgNumber}|{otp}",
                NotificationStatus = notified ? "Dispatched" : "Queued",
                MatchStatus = intake.MatchStatus,
                PhotoStored = _images.GetAll().Any(image => image.IntakeId == intake.Id),
                Resident = resident
            };
        }
    }
}
