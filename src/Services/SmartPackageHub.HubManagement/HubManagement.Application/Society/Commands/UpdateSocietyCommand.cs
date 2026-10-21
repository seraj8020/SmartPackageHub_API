using HubManagement.Application.Societies.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Domains.Abstractions.Models;

namespace HubManagement.Application.Societies.Commands
{
    public sealed record UpdateSocietyCommand(Guid SocietyId, string? Name, AddressData? Address, string? ContactEmail, string? Phone) : IUpdateCommand<SocietyCommandResult>;

    

    
}
