using HubManagement.Application.Support.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Support.Commands
{
    public sealed record CreateSupportRequestCommand(CreateSupportRequestDto Request)
        : ICreateCommand<SupportCommandResult>;
}
