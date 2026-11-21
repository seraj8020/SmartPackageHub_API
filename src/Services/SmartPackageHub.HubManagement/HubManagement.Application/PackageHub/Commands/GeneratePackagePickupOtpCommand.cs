using HubManagement.Application.PackageHub.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed record GeneratePackagePickupOtpCommand(Guid PackageId)
        : ICreateCommand<GeneratePackagePickupOtpResult>;
}
