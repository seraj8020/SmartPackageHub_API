using HubManagement.Application.Returns.DTOs;
using HubManagement.Application.Returns.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Returns.Commands
{
    public sealed class MarkPackageReturnCourierPickupCommandHandler
        : UpdateCommandHandler<MarkPackageReturnCourierPickupCommand, PackageReturnCommandResult>
    {
        private static readonly Action<ILogger, Guid, Guid, Exception?> LogCourierPickup =
            LoggerMessage.Define<Guid, Guid>(
                LogLevel.Information,
                new EventId(1, nameof(MarkPackageReturnCourierPickupCommandHandler)),
                "Return {ReturnId} marked courier-picked by staff {StaffUserId}");

        private readonly IRepository<PackageReturn> _returns;
        private readonly IRepository<PackageReturnImage> _images;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<AuditLog> _auditLogs;
        private readonly ILogger<MarkPackageReturnCourierPickupCommandHandler> _logger;

        public MarkPackageReturnCourierPickupCommandHandler(
            IRepository<PackageReturn> returns,
            IRepository<PackageReturnImage> images,
            IRepository<Package> packages,
            IRepository<Resident> residents,
            IRepository<Shelf> shelves,
            IRepository<AuditLog> auditLogs,
            ILogger<MarkPackageReturnCourierPickupCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _returns = returns;
            _images = images;
            _packages = packages;
            _residents = residents;
            _shelves = shelves;
            _auditLogs = auditLogs;
            _logger = logger;
        }

        public override async Task<PackageReturnCommandResult> Handle(
            MarkPackageReturnCourierPickupCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.StaffUserId == Guid.Empty)
                return PackageReturnCommandResult.Fail("Staff user id is required.");

            var packageReturn = await _returns.GetByIdAsync(request.ReturnId).ConfigureAwait(false);
            if (packageReturn is null)
                return PackageReturnCommandResult.NotFound("Return was not found.");

            var societyDenied = PackageReturnStaffGuard.EnsureSocietyAccess(
                packageReturn,
                request.StaffSocietyIds);
            if (societyDenied is not null)
                return societyDenied;

            var staffUserId = request.StaffUserId;

            if (packageReturn.Status != PackageReturnStatus.ReturnShelved)
                return PackageReturnCommandResult.Fail(
                    $"Only ReturnShelved returns can be marked picked by courier (current: {packageReturn.Status}).",
                    conflict: true);

            try
            {
                packageReturn.MarkCourierPickedUp(request.Request?.Note, request.Request?.Awb);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return PackageReturnCommandResult.Fail(ex.Message, conflict: ex is InvalidOperationException);
            }

            var package = await _packages.GetByIdAsync(packageReturn.PackageId).ConfigureAwait(false);
            if (package is not null)
                package.SyncReturnStatus(PackageReturnStatus.ReturnPickedByCourier);

            _auditLogs.Insert(AuditLog.Create(
                packageReturn.SocietyId,
                staffUserId,
                nameof(PackageReturn),
                packageReturn.Id,
                "ReturnPickedByCourier",
                DateTime.UtcNow,
                $"{{\"packageId\":\"{packageReturn.PackageId}\",\"awb\":\"{packageReturn.CourierPickupAwb}\"}}"));

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogCourierPickup(_logger, packageReturn.Id, staffUserId, null);

            string? shelfCode = null;
            if (packageReturn.ShelfId is Guid shelfId)
            {
                var shelf = await _shelves.GetByIdAsync(shelfId).ConfigureAwait(false);
                shelfCode = shelf?.Code;
            }

            var resident = await _residents.GetByIdAsync(packageReturn.ResidentId).ConfigureAwait(false);
            var photos = _images.FetchMulti(i => i.ReturnId == packageReturn.Id, noTracking: true).ToList();

            return PackageReturnCommandResult.Ok(
                PackageReturnMapper.ToDto(packageReturn, photos, package, resident, shelfCode),
                "Return marked as picked by courier.");
        }
    }
}
