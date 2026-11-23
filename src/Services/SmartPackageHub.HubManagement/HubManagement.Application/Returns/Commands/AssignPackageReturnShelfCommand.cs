using HubManagement.Application.Returns.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Returns.Commands
{
    public sealed record AssignPackageReturnShelfCommand(
        Guid ReturnId,
        Guid StaffUserId,
        IReadOnlyList<Guid> StaffSocietyIds,
        AssignPackageReturnShelfRequest? Request = null)
        : IUpdateCommand<PackageReturnCommandResult>;
}
