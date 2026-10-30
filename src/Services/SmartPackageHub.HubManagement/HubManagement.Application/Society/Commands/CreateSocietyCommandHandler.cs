using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Domains.Abstractions.Guards;
using SmartPackageHub.Domains.Abstractions.Models;

namespace HubManagement.Application.Societies.Commands
{
    public sealed class CreateSocietyCommandHandler : CreateCommandHandler<CreateSocietyCommand, SocietyCommandResult>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogCreatedSociety =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(CreateSocietyCommandHandler)),
                "Created society {SocietyId}");
        private readonly IRepository<Society> _societies;
        private readonly ILogger<CreateSocietyCommandHandler> _logger;

        public CreateSocietyCommandHandler(IRepository<Society> societies, ILogger<CreateSocietyCommandHandler> logger, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _societies = societies;
            _logger = logger;
        }

        public override async Task<SocietyCommandResult> Handle(CreateSocietyCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.Name))
                return new SocietyCommandResult(true, false, "Name is required", null);

            var addressResult = TryCreateAddress(request.Address);
            if (!addressResult.Succeeded)
                return new SocietyCommandResult(true, false, addressResult.Message, null);

            var society = Society.Create(request.Name.Trim(), addressResult.Address!, request.ContactEmail ?? string.Empty, request.Phone ?? string.Empty);
            _societies.Insert(society);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogCreatedSociety(_logger, society.Id, null);
            return new SocietyCommandResult(true, true, "Society created.", SocietyDetailsDto.From(society));
        }

        internal static (bool Succeeded, string Message, Address? Address) TryCreateAddress(AddressData? address)
        {
            if (address == null)
                return (false, "Address is required.", null);

            try
            {
                return (true, string.Empty, Address.Create(address));
            }
            catch (ArgumentException ex)
            {
                return (false, ex.Message, null);
            }
            catch (BusinessRuleException ex)
            {
                return (false, ex.Message, null);
            }
        }
    }
}
