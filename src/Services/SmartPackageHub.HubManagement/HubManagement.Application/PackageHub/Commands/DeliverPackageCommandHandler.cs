using HubManagement.Application.HubResidents.Services;
using HubManagement.Application.PackageHub.DTOs;
using HubManagement.Application.PackageHub.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Storage.Abstractions;

namespace HubManagement.Application.PackageHub.Commands
{

    public sealed class DeliverPackageCommandHandler : CreateCommandHandler<DeliverPackageCommand, PackageCommandResult>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogPackageDelivered =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(DeliverPackageCommandHandler)),
                "Delivered package {PackageId}");
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Delivery> _deliveries;
        private readonly IRepository<OtpLog> _otpLogs;
        private readonly IRepository<PackageCustodyHop> _custodyHops;
        private readonly IRepository<User> _users;
        private readonly IRepository<PackageDeliveryEvidence> _evidence;
        private readonly IRepository<VacationHold> _vacationHolds;
        private readonly IRepository<AuthorizedCollector> _collectors;
        private readonly IParcelPhotoStore _photos;
        private readonly ILogger<DeliverPackageCommandHandler> _logger;

        public DeliverPackageCommandHandler(
            IRepository<Package> packages,
            IRepository<Delivery> deliveries,
            IRepository<OtpLog> otpLogs,
            IRepository<PackageCustodyHop> custodyHops,
            IRepository<User> users,
            IRepository<PackageDeliveryEvidence> evidence,
            IRepository<VacationHold> vacationHolds,
            IRepository<AuthorizedCollector> collectors,
            IParcelPhotoStore photos,
            ILogger<DeliverPackageCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _deliveries = deliveries;
            _otpLogs = otpLogs;
            _custodyHops = custodyHops;
            _users = users;
            _evidence = evidence;
            _vacationHolds = vacationHolds;
            _collectors = collectors;
            _photos = photos;
            _logger = logger;
        }

        public override async Task<PackageCommandResult> Handle(DeliverPackageCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var payload = request.request ?? throw new ArgumentNullException(nameof(request));

            var gate = await ValidateDeliverAsync(request.packageId, request.StaffSocietyIds, payload, cancellationToken)
                .ConfigureAwait(false);
            if (gate.Error is not null)
                return gate.Error;

            var package = gate.Package!;
            var otpLog = gate.OtpLog!;
            var packageResidentId = gate.ResidentId;
            var utcNow = DateTime.UtcNow;

            package.DeliverPackage();
            var method = string.IsNullOrWhiteSpace(payload.Method) ? "CounterPickup" : payload.Method;
            var notes = BuildNotes(payload.Notes, package);
            var delivery = Delivery.Create(
                package.SocietyId,
                request.packageId,
                package.StaffUserId,
                packageResidentId,
                otpLog.Id,
                payload.BatchId,
                method,
                utcNow,
                notes);
            delivery.CompleteDelivery();
            _deliveries.Insert(delivery);

            var evidenceError = await PersistEvidenceAsync(delivery.Id, package.Id, payload, utcNow, cancellationToken)
                .ConfigureAwait(false);
            if (evidenceError is not null)
                return new PackageCommandResult(true, false, false, evidenceError, null);

            await InsertCustodyHopAsync(package, method, utcNow, cancellationToken).ConfigureAwait(false);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogPackageDelivered(_logger, package.Id, null);
            return new PackageCommandResult(true, false, true, "Package delivered.", null);
        }

        private async Task<(PackageCommandResult? Error, Package? Package, OtpLog? OtpLog, Guid ResidentId)> ValidateDeliverAsync(
            Guid packageId,
            IReadOnlyList<Guid> staffSocietyIds,
            DeliverPackageRequest payload,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(payload.Token))
                return (new PackageCommandResult(true, false, false, "OTP code is required.", null), null, null, Guid.Empty);

            var package = await _packages.GetByIdAsync(packageId).ConfigureAwait(false);
            if (package == null)
                return (new PackageCommandResult(false, false, false, "Package not found.", null), null, null, Guid.Empty);
            if (!PackageStaffGuard.HasSocietyAccess(package, staffSocietyIds))
                return (new PackageCommandResult(true, false, false, "Staff is not assigned to this package's society.", null, Forbidden: true), null, null, Guid.Empty);
            if (package.Status != PackageStatus.Dispatched)
                return (new PackageCommandResult(true, true, false, "Package must be dispatched before delivery.", null), null, null, Guid.Empty);
            if (package.ResidentId is not Guid packageResidentId || packageResidentId == Guid.Empty)
                return (new PackageCommandResult(true, false, false, "Package has no resident assigned.", null), null, null, Guid.Empty);

            // Door delivery blocked on vacation/hub hold. Counter pickup allowed for hub hold
            // or when an authorized collector is set during vacation hold.
            var deliverBlock = await PackageHoldLookup.GetDeliverBlockMessageAsync(
                    _vacationHolds,
                    _collectors,
                    package,
                    payload.Method,
                    DateTime.UtcNow)
                .ConfigureAwait(false);
            if (deliverBlock is not null)
            {
                return (new PackageCommandResult(
                    true,
                    false,
                    false,
                    deliverBlock,
                    null), null, null, Guid.Empty);
            }

            if (payload.SocietyId is Guid societyId && societyId != Guid.Empty && societyId != package.SocietyId)
                return (new PackageCommandResult(true, false, false, "Delivery details do not match the package.", null), null, null, Guid.Empty);
            if (payload.ResidentId is Guid residentId && residentId != Guid.Empty && residentId != packageResidentId)
                return (new PackageCommandResult(true, false, false, "Delivery details do not match the package.", null), null, null, Guid.Empty);
            if (payload.StaffUserId is Guid staffUserId && staffUserId != Guid.Empty && staffUserId != package.StaffUserId)
                return (new PackageCommandResult(true, false, false, "Delivery details do not match the package.", null), null, null, Guid.Empty);

            // Photo / signature are optional — OTP already proves the handover when the resident is in a hurry.
            var photoErr = DeliveryEvidenceSaver.ValidateBase64(payload.PhotoBase64, "Photo");
            if (photoErr is not null)
                return (new PackageCommandResult(true, false, false, photoErr, null), null, null, Guid.Empty);
            var sigErr = DeliveryEvidenceSaver.ValidateBase64(payload.SignatureBase64, "Signature");
            if (sigErr is not null)
                return (new PackageCommandResult(true, false, false, sigErr, null), null, null, Guid.Empty);

            var token = payload.Token.Trim();
            var utcNow = DateTime.UtcNow;
            // Match token/expiry in memory. Filtering owned Token.Value / Expiry.Value in SQL
            // has been unreliable with Npgsql + OwnsOne and returned null for valid OTPs.
            var candidates = await _otpLogs.ListAsync(
                    log =>
                        log.PackageId == package.Id &&
                        log.Purpose == "Pickup" &&
                        !log.IsUsed,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var otpLog = candidates
                .Where(log =>
                    log.Token is not null &&
                    string.Equals(log.Token.Value, token, StringComparison.Ordinal) &&
                    log.Expiry is not null &&
                    DateTime.SpecifyKind(log.Expiry.Value, DateTimeKind.Utc) > utcNow)
                .OrderByDescending(log => log.Expiry!.Value)
                .FirstOrDefault();

            if (otpLog == null || !otpLog.ValidateCollection(token))
                return (new PackageCommandResult(true, false, false, "Invalid, expired, or already-used OTP.", null), null, null, Guid.Empty);

            return (null, package, otpLog, packageResidentId);
        }

        private async Task<string?> PersistEvidenceAsync(
            Guid deliveryId,
            Guid packageId,
            DeliverPackageRequest payload,
            DateTime capturedAtUtc,
            CancellationToken cancellationToken)
        {
            var photoError = await DeliveryEvidenceSaver.TrySaveAsync(
                    _photos,
                    _evidence,
                    deliveryId,
                    packageId,
                    DeliveryEvidenceKind.Photo,
                    payload.PhotoBase64,
                    null,
                    capturedAtUtc,
                    cancellationToken)
                .ConfigureAwait(false);
            if (photoError is not null)
                return photoError;

            return await DeliveryEvidenceSaver.TrySaveAsync(
                    _photos,
                    _evidence,
                    deliveryId,
                    packageId,
                    DeliveryEvidenceKind.Signature,
                    payload.SignatureBase64,
                    payload.SignedByName,
                    capturedAtUtc,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task InsertCustodyHopAsync(
            Package package,
            string method,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            var staffName = await PackageCustodyRecorder
                .ResolveUsernameAsync(_users, package.StaffUserId, cancellationToken)
                .ConfigureAwait(false);
            PackageCustodyRecorder.Insert(
                _custodyHops,
                package.Id,
                package.SocietyId,
                PackageCustodyHopKinds.Delivered,
                utcNow,
                fromZone: PackageCustodyRecorder.LatestZone(_custodyHops, package.Id),
                toZone: PackageCustodyRecorder.DoorZone(package.TowerWing, package.FlatNumber.Value),
                locationLabel: package.FlatNumber.Value,
                staffUserId: package.StaffUserId,
                staffUsername: staffName,
                notes: method);
        }

        private static string BuildNotes(string? notes, Package package)
        {
            var value = string.IsNullOrWhiteSpace(notes)
                ? $"Handed to '{package.RecipientName}' at Flat Number '{package.FlatNumber}'"
                : notes.Trim();
            return value.Length > 500 ? value[..500] : value;
        }
    }
}
