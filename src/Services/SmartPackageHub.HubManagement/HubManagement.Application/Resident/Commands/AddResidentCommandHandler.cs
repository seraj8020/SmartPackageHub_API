using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Application.HubResidents.Services;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed class AddResidentCommandHandler
        : CreateCommandHandler<AddResidentCommand, AddResidentResult>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogCreatedResident =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(AddResidentCommandHandler)),
                "Created Resident: {ResidentId}");

        private readonly IRepository<Resident> _repository;
        private readonly IRepository<User> _users;
        private readonly ILogger<AddResidentCommandHandler> _logger;

        public AddResidentCommandHandler(
            IRepository<Resident> repository,
            IRepository<User> users,
            ILogger<AddResidentCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _repository = repository;
            _users = users;
            _logger = logger;
        }

        public override async Task<AddResidentResult> Handle(
            AddResidentCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await ResidentProvisioner.ProvisionAsync(
                _repository,
                _users,
                UnitOfWork!,
                request.societyId,
                request.userId,
                request.fullname,
                request.phoneNo,
                request.towerWing,
                request.flatnumber,
                request.isMember,
                request.picUrl,
                request.isActive,
                cancellationToken).ConfigureAwait(false);

            if (result.Succeeded && result.Id is Guid residentId)
                LogCreatedResident(_logger, residentId, null);

            return result;
        }
    }
}
