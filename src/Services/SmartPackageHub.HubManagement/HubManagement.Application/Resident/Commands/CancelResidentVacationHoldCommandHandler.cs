using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed class CancelResidentVacationHoldCommandHandler
        : CreateCommandHandler<CancelResidentVacationHoldCommand>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogCancelled =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(CancelResidentVacationHoldCommandHandler)),
                "Cancelled vacation hold for resident {ResidentId}");

        private readonly IRepository<VacationHold> _holds;
        private readonly IRepository<Resident> _residents;
        private readonly ILogger<CancelResidentVacationHoldCommandHandler> _logger;

        public CancelResidentVacationHoldCommandHandler(
            IRepository<VacationHold> holds,
            IRepository<Resident> residents,
            ILogger<CancelResidentVacationHoldCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _holds = holds;
            _residents = residents;
            _logger = logger;
        }

        protected override async Task<Guid> HandleAsync(CancelResidentVacationHoldCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident is null)
                throw new InvalidOperationException("Resident was not found.");

            var holds = await _holds.ListAsync(
                hold => hold.ResidentId == request.ResidentId && hold.IsActive).ConfigureAwait(false);
            var active = holds.OrderByDescending(hold => hold.ToDate).FirstOrDefault();
            if (active is null)
                throw new InvalidOperationException("No active vacation hold to cancel.");

            active.Cancel();
            _holds.Update(active);
            await UnitOfWork!.CommitAsync().ConfigureAwait(false);
            LogCancelled(_logger, request.ResidentId, null);
            return active.Id;
        }
    }
}
