using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Commands
{
    public sealed class DeleteSocietyCommandHandler : UpdateCommandHandler<DeleteSocietyCommand, DeleteCommandResult>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogDeleteSociety =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(DeleteSocietyCommandHandler)),
                "Deleted society {SocietyId}");
        private readonly IRepository<Society> _societies;
        private readonly ILogger<DeleteSocietyCommandHandler> _logger;

        public DeleteSocietyCommandHandler(IRepository<Society> societies, ILogger<DeleteSocietyCommandHandler> logger, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _societies = societies;
            _logger = logger;
        }

        public override async Task<DeleteCommandResult> Handle(DeleteSocietyCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var society = await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            if (society == null)
                return new DeleteCommandResult(false);

            _societies.Delete(society);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogDeleteSociety(_logger, society.Id, null);
            return new DeleteCommandResult(true);
        }
    }
}
