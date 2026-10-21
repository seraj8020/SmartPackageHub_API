using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Societies.Commands
{
    public sealed class UpdateSocietyCommandHandler : UpdateCommandHandler<UpdateSocietyCommand, SocietyCommandResult>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogUpdatedSociety =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(UpdateSocietyCommandHandler)),
                "Updated society {SocietyId}");
        private readonly IRepository<Society> _societies;
        private readonly ILogger<UpdateSocietyCommandHandler> _logger;

        public UpdateSocietyCommandHandler(IRepository<Society> societies, ILogger<UpdateSocietyCommandHandler> logger, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _societies = societies;
            _logger = logger;
        }

        public override async Task<SocietyCommandResult> Handle(UpdateSocietyCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var society = await _societies.GetByIdAsync(request.SocietyId).ConfigureAwait(false);
            if (society == null)
                return new SocietyCommandResult(false, false, "Society was not found.", null);

            if (!string.IsNullOrWhiteSpace(request.Name))
                society.Rename(request.Name);

            if (request.Address != null)
            {
                var addressResult = CreateSocietyCommandHandler.TryCreateAddress(request.Address);
                if (!addressResult.Succeeded)
                    return new SocietyCommandResult(true, false, addressResult.Message, null);

                society.ChangeAddress(addressResult.Address!);
            }

            society.ChangeContact(request.ContactEmail, request.Phone);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogUpdatedSociety(_logger, society.Id, null);
            return new SocietyCommandResult(true, true, "Society updated.", SocietyDetailsDto.From(society));
        }
    }
}
