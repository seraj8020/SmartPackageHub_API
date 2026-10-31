using HubManagement.Application.PackageHub.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

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
        private readonly ILogger<DeliverPackageCommandHandler> _logger;

        public DeliverPackageCommandHandler(
            IRepository<Package> packages,
            IRepository<Delivery> deliveries,
            IRepository<OtpLog> otpLogs,
            ILogger<DeliverPackageCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _deliveries = deliveries;
            _otpLogs = otpLogs;
            _logger = logger;
        }

        public override async Task<PackageCommandResult> Handle(DeliverPackageCommand command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            var request = command.request;
            if (request is null)
                throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(request.Token))
                return new PackageCommandResult(true, false, false, "OTP code is required.", null);

            var package = await _packages.GetByIdAsync(command.packageId).ConfigureAwait(false);
            if (package == null)
                return new PackageCommandResult(false, false, false, "Package not found.", null);
            if (package.Status != PackageStatus.Dispatched)
                return new PackageCommandResult(true, true, false, "Package must be dispatched before delivery.", null);
            if (package.ResidentId is not Guid packageResidentId || packageResidentId == Guid.Empty)
                return new PackageCommandResult(true, false, false, "Package has no resident assigned.", null);

            // Prefer package truth; only reject when client explicitly sends a mismatched override.
            if (request.SocietyId is Guid societyId && societyId != Guid.Empty && societyId != package.SocietyId)
                return new PackageCommandResult(true, false, false, "Delivery details do not match the package.", null);
            if (request.ResidentId is Guid residentId && residentId != Guid.Empty && residentId != packageResidentId)
                return new PackageCommandResult(true, false, false, "Delivery details do not match the package.", null);
            if (request.StaffUserId is Guid staffUserId && staffUserId != Guid.Empty && staffUserId != package.StaffUserId)
                return new PackageCommandResult(true, false, false, "Delivery details do not match the package.", null);

            var token = request.Token.Trim();
            var utcNow = DateTime.UtcNow;
            var otpLog = await _otpLogs.FirstOrDefaultAsync(log =>
                log.PackageId == package.Id &&
                log.Purpose == "Pickup" &&
                !log.IsUsed &&
                log.Token != null &&
                log.Token.Value == token &&
                log.Expiry != null &&
                log.Expiry.Value > utcNow).ConfigureAwait(false);

            if (otpLog == null || !otpLog.ValidateCollection(token))
                return new PackageCommandResult(true, false, false, "Invalid, expired, or already-used OTP.", null);

            package.DeliverPackage();
            var delivery = Delivery.Create(
                package.SocietyId,
                command.packageId,
                package.StaffUserId,
                packageResidentId,
                otpLog.Id,
                request.BatchId,
                string.IsNullOrWhiteSpace(request.Method) ? "CounterPickup" : request.Method,
                utcNow,
                request.Notes ?? $"Handed to '{package.RecipientName}' at Flat Number '{package.FlatNumber}'");
            delivery.CompleteDelivery();
            _deliveries.Insert(delivery);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogPackageDelivered(_logger, package.Id, null);
            return new PackageCommandResult(true, false, true, "Package delivered.", null);
        }
    }
}
