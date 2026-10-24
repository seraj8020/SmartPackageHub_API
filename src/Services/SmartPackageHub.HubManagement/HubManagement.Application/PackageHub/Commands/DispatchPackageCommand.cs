using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed record DispatchPackageCommand(Guid packageId) : ICreateCommand<bool>;
}
