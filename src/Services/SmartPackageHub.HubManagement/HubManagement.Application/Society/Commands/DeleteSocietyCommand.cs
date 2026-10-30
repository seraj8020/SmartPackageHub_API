using HubManagement.Application.Societies.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Societies.Commands
{
    public sealed record DeleteSocietyCommand(Guid SocietyId) : IUpdateCommand<DeleteCommandResult>;
}
