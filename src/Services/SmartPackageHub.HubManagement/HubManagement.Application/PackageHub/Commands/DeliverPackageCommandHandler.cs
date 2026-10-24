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
            ArgumentNullException.ThrowIfNull(command.request);
            var request = command.request;
            if (string.IsNullOrWhiteSpace(request.Token))
                return new PackageCommandResult(true, false, false, "OTP code is required.", null);

            var package = await _packages.GetByIdAsync(command.packageId).ConfigureAwait(false);
            if (package == null)
                return new PackageCommandResult(false, false, false, "Package not found.", null);
            if (package.Status != PackageStatus.Dispatched)
                return new PackageCommandResult(true, true, false, "Package must be dispatched before delivery.", null);
            if (request.SocietyId != package.SocietyId ||
                request.ResidentId != package.ResidentId ||
                request.StaffUserId != package.StaffUserId)
                return new PackageCommandResult(true, false, false, "Delivery details do not match the package.", null);

            var otpLogs = _otpLogs
                .FetchMulti(log =>
                    log.PackageId == package.Id &&
                    log.ResidentId == request.ResidentId &&
                    log.SocietyId == request.SocietyId &&
                    log.Purpose == "Pickup" &&
                    !log.IsUsed)
                .ToList();
            var otpLog = otpLogs.FirstOrDefault(log => log.ValidateCollection(request.Token.Trim()));
            if (otpLog == null)
                return new PackageCommandResult(true, false, false, "Invalid, expired, or already-used OTP.", null);

            package.DeliverPackage();
            //Create a new Delivery entity with all the required details and mark it as delivered
            var delivery = Delivery.Create(
                request.SocietyId,
                command.packageId,
                request.StaffUserId,
                request.ResidentId,
                otpLog.Id,
                request.BatchId,
                string.IsNullOrWhiteSpace(request.Method) ? "CounterPickup" : request.Method,
                DateTime.UtcNow,
                request.Notes ?? $"Handed to '{package.RecipientName}' at Flat Number '{package.FlatNumber}'");
            delivery.CompleteDelivery();
            _deliveries.Insert(delivery);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogPackageDelivered(_logger, package.Id, null);
            return new PackageCommandResult(true, false, true, "Package delivered.", PackageDetailsDto.From(package));
        }
    }
}
