using HubManagement.Application.Returns.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Returns.Commands
{
    public sealed record MarkPackageReturnReceivedCommand(
        Guid ReturnId,
        Guid StaffUserId,
        IReadOnlyList<Guid> StaffSocietyIds,
        MarkPackageReturnReceivedRequest? Request = null)
        : IUpdateCommand<PackageReturnCommandResult>;
}
