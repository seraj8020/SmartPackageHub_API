using HubManagement.Application.Returns.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Returns.Commands
{
    public sealed record MarkPackageReturnCourierPickupCommand(
        Guid ReturnId,
        Guid StaffUserId,
        IReadOnlyList<Guid> StaffSocietyIds,
        MarkPackageReturnCourierPickupRequest? Request = null)
        : IUpdateCommand<PackageReturnCommandResult>;
}
