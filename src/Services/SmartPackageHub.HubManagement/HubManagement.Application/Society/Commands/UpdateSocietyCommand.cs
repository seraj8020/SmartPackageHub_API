using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Domains.Abstractions.Models;

namespace HubManagement.Application.Societies.Commands
{
    public sealed record UpdateSocietyCommand(Guid SocietyId, string? Name, AddressData? Address, string? ContactEmail, string? Phone) : IUpdateCommand<SocietyCommandResult>;

    public sealed class UpdateSocietyCommandHandler : UpdateCommandHandler<UpdateSocietyCommand, SocietyCommandResult>
    {
        private readonly IRepository<Society> _societies;
        private readonly ILogger<UpdateSocietyCommandHandler> _logger;

        public UpdateSocietyCommandHandler(IRepository<Society> societies, ILogger<UpdateSocietyCommandHandler> logger, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _societies = societies;
            _logger = logger;
        }

        public override async Task<SocietyCommandResult> Handle(UpdateSocietyCommand request, CancellationToken cancellationToken)
        {
            var society = await _societies.GetByIdAsync(request.SocietyId);
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
            await UnitOfWork!.CommitAsync(cancellationToken);
            _logger.LogInformation("Updated society {SocietyId}", society.Id);
            return new SocietyCommandResult(true, true, "Society updated.", SocietyDetailsDto.From(society));
        }
    }

    
}
