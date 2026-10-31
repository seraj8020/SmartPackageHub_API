using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Guards;

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
        private readonly ILogger<AddResidentCommandHandler> _logger;

        public AddResidentCommandHandler(
            IRepository<Resident> repository,
            ILogger<AddResidentCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _repository = repository;
            _logger = logger;
        }

        public override async Task<AddResidentResult> Handle(
            AddResidentCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.societyId == Guid.Empty || request.userId == Guid.Empty)
                return new AddResidentResult(false, false, null, "Society and user are required.");

            if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.phoneNo, out var phone))
                return new AddResidentResult(false, false, null, "Enter a valid 10-digit mobile number.");

            var duplicate = _repository.GetAll().AsEnumerable().Any(resident =>
                resident.SocietyId == request.societyId &&
                PhoneNumberGuard.TryNormalizeIndiaMobile(resident.PhoneNumber, out var stored) &&
                string.Equals(stored, phone, StringComparison.Ordinal));

            if (duplicate)
            {
                return new AddResidentResult(
                    false,
                    true,
                    null,
                    "A resident with this mobile number is already registered in this society.");
            }

            try
            {
                var resident = Resident.Create(
                    request.societyId,
                    request.userId,
                    request.fullname,
                    phone,
                    request.towerWing,
                    request.flatnumber,
                    request.isMember,
                    request.picUrl,
                    request.isActive);
                _repository.Insert(resident);
                await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
                LogCreatedResident(_logger, resident.Id, null);
                return new AddResidentResult(true, false, resident.Id, "Resident registered.");
            }
            catch (ArgumentException ex)
            {
                return new AddResidentResult(false, false, null, ex.Message);
            }
        }
    }
}
