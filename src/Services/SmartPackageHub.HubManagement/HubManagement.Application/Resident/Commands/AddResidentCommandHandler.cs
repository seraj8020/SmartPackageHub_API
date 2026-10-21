using HubManagement.Application.Societies.Commands;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed class AddResidentCommandHandler : CreateCommandHandler<AddResidentCommand>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogCreatedResident =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(AddResidentCommandHandler)),
                "Created Resident: {@Resident}");
        private readonly IRepository<Resident> _repository;
        private readonly ILogger<AddResidentCommandHandler> _logger;
        public AddResidentCommandHandler(IRepository<Resident> repository, ILogger<AddResidentCommandHandler> logger,
        IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _repository = repository;
            _logger = logger;
        }

        protected override async Task<Guid> HandleAsync(AddResidentCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var residentCreated = Resident.Create(request.societyId, request.userId, request.fullname, request.phoneNo, request.towerWing, request.flatnumber, request.isMember, request.picUrl, request.isActive);
            _repository.Insert(residentCreated);
            await UnitOfWork!.CommitAsync().ConfigureAwait(false);
            LogCreatedResident(_logger, residentCreated.Id, null);
            return residentCreated.Id;
        }
    }
}
