using HubManagement.Application.PackageHub.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed record TransferPackageZoneCommand(
        Guid PackageId,
        TransferPackageZoneRequest Request,
        Guid StaffUserId,
        IReadOnlyList<Guid> StaffSocietyIds) : ICreateCommand<PackageCommandResult>;
}
