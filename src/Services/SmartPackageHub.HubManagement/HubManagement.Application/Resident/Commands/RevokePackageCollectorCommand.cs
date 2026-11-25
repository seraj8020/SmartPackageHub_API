using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed record RevokePackageCollectorCommand(Guid ResidentId, Guid PackageId) : CreateCommand;
}
