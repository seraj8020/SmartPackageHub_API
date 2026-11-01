using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed class SaveResidentVacationHoldCommandHandler : CreateCommandHandler<SaveResidentVacationHoldCommand>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogSaved =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(SaveResidentVacationHoldCommandHandler)),
                "Saved vacation hold {HoldId}");

        private readonly IRepository<VacationHold> _holds;
        private readonly IRepository<Resident> _residents;
        private readonly ILogger<SaveResidentVacationHoldCommandHandler> _logger;

        public SaveResidentVacationHoldCommandHandler(
            IRepository<VacationHold> holds,
            IRepository<Resident> residents,
            ILogger<SaveResidentVacationHoldCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _holds = holds;
            _residents = residents;
            _logger = logger;
        }

        protected override async Task<Guid> HandleAsync(SaveResidentVacationHoldCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var resident = await _residents.GetByIdAsync(request.residentId).ConfigureAwait(false);
            if (resident is null)
                throw new InvalidOperationException("Resident was not found.");
            if (resident.SocietyId != request.societyId)
                throw new InvalidOperationException("Society does not match this resident.");

            var start = request.startDate.Date;
            var end = request.endDate.Date;
            if (end <= start)
                throw new ArgumentException("End date must be after the start date.", nameof(request));

            var existing = await _holds.ListAsync(
                hold => hold.ResidentId == request.residentId && hold.IsActive).ConfigureAwait(false);

            var current = existing.OrderByDescending(hold => hold.ToDate).FirstOrDefault();
            if (current is not null)
            {
                var overlap = existing.Any(hold =>
                    hold.Id != current.Id &&
                    hold.IsActive &&
                    start <= hold.ToDate &&
                    end >= hold.FromDate);
                if (overlap)
                    throw new InvalidOperationException("A second hold cannot overlap this one.");

                current.Reschedule(start, end);
                _holds.Update(current);
                await UnitOfWork!.CommitAsync().ConfigureAwait(false);
                LogSaved(_logger, current.Id, null);
                return current.Id;
            }

            if (start < DateTime.UtcNow.Date)
                throw new ArgumentException("Vacation hold cannot start in the past.", nameof(request));

            var created = VacationHold.Create(
                request.societyId,
                request.residentId,
                start,
                end,
                request.isActive);
            _holds.Insert(created);
            await UnitOfWork!.CommitAsync().ConfigureAwait(false);
            LogSaved(_logger, created.Id, null);
            return created.Id;
        }
    }
}
