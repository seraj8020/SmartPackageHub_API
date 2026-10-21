using HubManagement.Application.Societies.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubUsers.Commands
{
    public sealed record DeleteUserCommand(Guid UserId) : IUpdateCommand<DeleteCommandResult>;

    
}
