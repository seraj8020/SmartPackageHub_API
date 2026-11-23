using HubManagement.Application.Returns.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Returns.Commands
{
    public sealed record RequestPackageReturnCommand(Guid PackageId, RequestPackageReturnRequest Request)
        : ICreateCommand<PackageReturnCommandResult>;
}
