using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed class SaveResidentVacationHoldCommandHandler : CreateCommandHandler<SaveResidentVacationHoldCommand>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogCreatedResident =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(SaveResidentVacationHoldCommandHandler)),
                "Created VacationHold: {@Resident}");
        private readonly IRepository<VacationHold> _repository;
        private readonly ILogger<SaveResidentVacationHoldCommandHandler> _logger;
        public SaveResidentVacationHoldCommandHandler(IRepository<VacationHold> repository, ILogger<SaveResidentVacationHoldCommandHandler> logger,
        IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _repository = repository;
            _logger = logger;
        }

        protected override async Task<Guid> HandleAsync(SaveResidentVacationHoldCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var vacationCreated = VacationHold.Create(request.societyId,  request.residentId, request.startDate, request.endDate, request.isActive);
            _repository.Insert(vacationCreated);
            await UnitOfWork!.CommitAsync().ConfigureAwait(false);
            LogCreatedResident(_logger, vacationCreated.Id, null);
            return vacationCreated.Id;
        }
    }
}
