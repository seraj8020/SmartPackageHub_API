using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed record AuthorizePackageCollectorCommand(
        Guid ResidentId,
        Guid PackageId,
        string CollectorName,
        string? CollectorPhone,
        string? Note,
        DateTime? ExpiresAtUtc) : CreateCommand;
}
