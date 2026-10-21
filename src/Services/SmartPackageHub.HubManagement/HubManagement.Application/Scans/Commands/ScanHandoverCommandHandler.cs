using HubManagement.Application.PackageHub.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Scans.Commands
{
    public sealed class ScanHandoverCommandHandler : UpdateCommandHandler<ScanHandoverCommand, PackageCommandResult>
    {
        private static readonly Action<ILogger, Guid, string?, Exception?> LogScanHandover =
            LoggerMessage.Define<Guid, string?>(
                LogLevel.Information,
                new EventId(1, nameof(ScanHandoverCommandHandler)),
                "Handed over package {PackageId} to {Staff}");
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Delivery> _deliveries;
        private readonly ILogger<ScanHandoverCommandHandler> _logger;

        public ScanHandoverCommandHandler(
            IRepository<Package> packages,
            IRepository<Delivery> deliveries,
            ILogger<ScanHandoverCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _deliveries = deliveries;
            _logger = logger;
        }

        public override async Task<PackageCommandResult> Handle(ScanHandoverCommand command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            var code = command.Request.Code?.Trim();
            if (string.IsNullOrWhiteSpace(code))
                return new PackageCommandResult(true, false, false, "Code is required", null);

            var packageId = Guid.TryParse(code, out var parsedId) ? parsedId : Guid.Empty;
            var package = _packages.FetchMulti(item =>
                    item.Id == packageId ||
                    item.PkgNumber == code ||
                    item.CourierAwb == code ||
                    item.SphTrackingNumber.Value == code)
                .FirstOrDefault();
            if (package == null)
                return new PackageCommandResult(false, false, false, "Package not found", null);
            if (package.Status == PackageStatus.Delivered)
                return new PackageCommandResult(true, false, false, "Package already handed over", null);
            if (package.ResidentId is not Guid residentId || residentId == Guid.Empty)
                return new PackageCommandResult(true, false, false, "Package is not assigned to a resident", null);

            package.MarkCollected();
            var delivery = Delivery.Create(
                package.SocietyId,
                package.Id,
                package.StaffUserId,
                residentId,
                null,
                null,
                "Handover",
                DateTime.UtcNow,
                string.IsNullOrWhiteSpace(command.Request.Notes) ? "Handed over via scan" : command.Request.Notes.Trim());
            delivery.CompleteDelivery();
            _deliveries.Insert(delivery);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogScanHandover(_logger, package.Id, package.StaffUserId.ToString(), null);
            return new PackageCommandResult(true, false, true, "Package handed over", PackageDetailsDto.From(package));
        }
    }
}
