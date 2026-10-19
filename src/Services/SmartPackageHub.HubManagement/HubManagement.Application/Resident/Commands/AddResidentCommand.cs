using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed record AddResidentCommand(Guid societyId, Guid userId, string fullname, string phoneNo, string towerWing, string flatnumber, bool isMember, string picUrl, bool isActive) : CreateCommand;
    public sealed class AddResidentCommandHandler : CreateCommandHandler<AddResidentCommand>
    {
        private readonly IRepository<Resident> _repository;
        private readonly ILogger<AddResidentCommandHandler> logger;
        public AddResidentCommandHandler(IRepository<Resident> repository, ILogger<AddResidentCommandHandler> _logger,
        IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _repository = repository;
            logger = _logger;
        }

        protected override async Task<Guid> HandleAsync(AddResidentCommand request)
        {
            var residentCreated = Resident.Create(request.societyId, request.userId, request.fullname, request.phoneNo, request.towerWing, request.flatnumber, request.isMember, request.picUrl, request.isActive);
            //Guard.Against.NotFound(created);
            _repository.Insert(residentCreated);
            await UnitOfWork.CommitAsync();
            logger.LogInformation("Created Resident: {@Resident}", residentCreated.Id);
            return residentCreated.Id;
        }
    }
}
