using HubManagement.Application.PackageHub.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed record ReShelveAfterFailedDeliveryCommand(
        Guid PackageId,
        IReadOnlyList<Guid> StaffSocietyIds)
        : ICreateCommand<PackageCommandResult>;
}
