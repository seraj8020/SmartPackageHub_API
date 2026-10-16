using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Commands
{
    public sealed record DeleteSocietyCommand(Guid SocietyId) : IUpdateCommand<DeleteCommandResult>;

    public sealed class DeleteSocietyCommandHandler : UpdateCommandHandler<DeleteSocietyCommand, DeleteCommandResult>
    {
        private readonly IRepository<Society> _societies;
        private readonly ILogger<DeleteSocietyCommandHandler> _logger;

        public DeleteSocietyCommandHandler(IRepository<Society> societies, ILogger<DeleteSocietyCommandHandler> logger, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _societies = societies;
            _logger = logger;
        }

        public override async Task<DeleteCommandResult> Handle(DeleteSocietyCommand request, CancellationToken cancellationToken)
        {
            var society = await _societies.GetByIdAsync(request.SocietyId);
            if (society == null)
                return new DeleteCommandResult(false);

            _societies.Delete(society);
            await UnitOfWork!.CommitAsync(cancellationToken);
            _logger.LogInformation("Deleted society {SocietyId}", society.Id);
            return new DeleteCommandResult(true);
        }
    }
}
