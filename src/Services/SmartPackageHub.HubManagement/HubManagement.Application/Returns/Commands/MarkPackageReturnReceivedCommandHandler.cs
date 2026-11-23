using HubManagement.Application.Returns.DTOs;
using HubManagement.Application.Returns.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Returns.Commands
{
    public sealed class MarkPackageReturnReceivedCommandHandler
        : UpdateCommandHandler<MarkPackageReturnReceivedCommand, PackageReturnCommandResult>
    {
        private static readonly Action<ILogger, Guid, Guid, Exception?> LogReceived =
            LoggerMessage.Define<Guid, Guid>(
                LogLevel.Information,
                new EventId(1, nameof(MarkPackageReturnReceivedCommandHandler)),
                "Return {ReturnId} marked received by staff {StaffUserId}");

        private readonly IRepository<PackageReturn> _returns;
        private readonly IRepository<PackageReturnImage> _images;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<AuditLog> _auditLogs;
        private readonly ILogger<MarkPackageReturnReceivedCommandHandler> _logger;

        public MarkPackageReturnReceivedCommandHandler(
            IRepository<PackageReturn> returns,
            IRepository<PackageReturnImage> images,
            IRepository<Package> packages,
            IRepository<Resident> residents,
            IRepository<AuditLog> auditLogs,
            ILogger<MarkPackageReturnReceivedCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _returns = returns;
            _images = images;
            _packages = packages;
            _residents = residents;
            _auditLogs = auditLogs;
            _logger = logger;
        }

        public override async Task<PackageReturnCommandResult> Handle(
            MarkPackageReturnReceivedCommand request,
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

            if (packageReturn.Status != PackageReturnStatus.ReturnRequested)
                return PackageReturnCommandResult.Fail(
                    $"Only ReturnRequested returns can be marked received (current: {packageReturn.Status}).",
                    conflict: true);

            try
            {
                packageReturn.MarkReceived(staffUserId);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return PackageReturnCommandResult.Fail(ex.Message, conflict: ex is InvalidOperationException);
            }

            var package = await _packages.GetByIdAsync(packageReturn.PackageId).ConfigureAwait(false);
            if (package is not null)
                package.SyncReturnStatus(PackageReturnStatus.ReturnReceived);

            _auditLogs.Insert(AuditLog.Create(
                packageReturn.SocietyId,
                staffUserId,
                nameof(PackageReturn),
                packageReturn.Id,
                "ReturnReceived",
                DateTime.UtcNow,
                $"{{\"packageId\":\"{packageReturn.PackageId}\"}}"));

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogReceived(_logger, packageReturn.Id, staffUserId, null);

            var resident = await _residents.GetByIdAsync(packageReturn.ResidentId).ConfigureAwait(false);
            var photos = _images.FetchMulti(i => i.ReturnId == packageReturn.Id, noTracking: true).ToList();

            return PackageReturnCommandResult.Ok(
                PackageReturnMapper.ToDto(packageReturn, photos, package, resident),
                "Return marked as received.");
        }
    }
}
