using HubManagement.Application.Intake.Converters;
using HubManagement.Application.Intake.Helpers;
using HubManagement.Application.Returns.DTOs;
using HubManagement.Application.Returns.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Returns.Commands
{
    public sealed class AssignPackageReturnShelfCommandHandler
        : UpdateCommandHandler<AssignPackageReturnShelfCommand, PackageReturnCommandResult>
    {
        private static readonly Action<ILogger, string, Guid, Exception?> LogShelved =
            LoggerMessage.Define<string, Guid>(
                LogLevel.Information,
                new EventId(1, nameof(AssignPackageReturnShelfCommandHandler)),
                "Assigned return shelf {ShelfCode} to return {ReturnId}");

        private readonly IRepository<PackageReturn> _returns;
        private readonly IRepository<PackageReturnImage> _images;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Shelf> _shelves;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<AuditLog> _auditLogs;
        private readonly ILogger<AssignPackageReturnShelfCommandHandler> _logger;

        public AssignPackageReturnShelfCommandHandler(
            IRepository<PackageReturn> returns,
            IRepository<PackageReturnImage> images,
            IRepository<Package> packages,
            IRepository<Shelf> shelves,
            IRepository<Resident> residents,
            IRepository<AuditLog> auditLogs,
            ILogger<AssignPackageReturnShelfCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _returns = returns;
            _images = images;
            _packages = packages;
            _shelves = shelves;
            _residents = residents;
            _auditLogs = auditLogs;
            _logger = logger;
        }

        public override async Task<PackageReturnCommandResult> Handle(
            AssignPackageReturnShelfCommand request,
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

            if (packageReturn.Status is not (PackageReturnStatus.ReturnReceived or PackageReturnStatus.ReturnShelved))
                return PackageReturnCommandResult.Fail(
                    $"Return must be received before assigning a shelf (current: {packageReturn.Status}).",
                    conflict: true);

            var package = await _packages.GetByIdAsync(packageReturn.PackageId).ConfigureAwait(false);
            if (package is null)
                return PackageReturnCommandResult.NotFound("Package was not found.");

            var requestedCode = IntakeText.Clean(request.Request?.ShelfCode, 50);
            var societyShelves = _shelves.FetchMulti(s => s.SocietyId == packageReturn.SocietyId).ToList();
            var shelf = requestedCode is null
                ? null
                : societyShelves.FirstOrDefault(s =>
                    string.Equals(s.Code, requestedCode, StringComparison.OrdinalIgnoreCase));
            var wasSuggested = shelf is null;

            if (shelf is not null &&
                !shelf.IsColdStorage &&
                !string.IsNullOrWhiteSpace(package.TowerWing) &&
                !ResidentMatcher.TowerEquals(shelf.TowerOrWing, package.TowerWing))
            {
                return PackageReturnCommandResult.Fail(
                    $"Shelf {shelf.Code} belongs to tower {shelf.TowerOrWing}, not resident tower {package.TowerWing}.");
            }

            if (shelf is null)
            {
                var societyPackages = _packages.FetchMulti(p => p.SocietyId == package.SocietyId).ToList();
                shelf = ShelfSuggester.Suggest(
                    societyShelves,
                    societyPackages,
                    package.SocietyId,
                    package.IsColdStorage,
                    package.TowerWing);
            }

            if (shelf is null)
            {
                return PackageReturnCommandResult.Fail(
                    "No shelf is configured for this resident's tower. Return remains received.");
            }

            try
            {
                packageReturn.AssignReturnShelf(shelf.Id);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return PackageReturnCommandResult.Fail(ex.Message, conflict: ex is InvalidOperationException);
            }

            package.AssignShelf(shelf.Id);
            package.SyncReturnStatus(PackageReturnStatus.ReturnShelved);

            _auditLogs.Insert(AuditLog.Create(
                packageReturn.SocietyId,
                staffUserId,
                nameof(PackageReturn),
                packageReturn.Id,
                "ReturnShelved",
                DateTime.UtcNow,
                $"{{\"shelfCode\":\"{shelf.Code}\",\"packageId\":\"{package.Id}\"}}"));

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogShelved(_logger, shelf.Code, packageReturn.Id, null);

            var resident = await _residents.GetByIdAsync(packageReturn.ResidentId).ConfigureAwait(false);
            var photos = _images.FetchMulti(i => i.ReturnId == packageReturn.Id, noTracking: true).ToList();
            var message = wasSuggested
                ? $"Requested shelf was unavailable. Return shelved at {shelf.Code}."
                : $"Return shelved at {shelf.Code}.";

            return PackageReturnCommandResult.Ok(
                PackageReturnMapper.ToDto(packageReturn, photos, package, resident, shelf.Code),
                message);
        }
    }
}
