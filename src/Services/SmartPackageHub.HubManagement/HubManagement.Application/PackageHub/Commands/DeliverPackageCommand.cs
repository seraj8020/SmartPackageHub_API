using HubManagement.Application.PackageHub.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed record DeliverPackageCommand(Guid packageId, DeliverPackageRequest request) : ICreateCommand<PackageCommandResult>;
}
