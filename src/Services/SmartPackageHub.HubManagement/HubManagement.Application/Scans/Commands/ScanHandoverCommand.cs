using HubManagement.Application.PackageHub.DTOs;
using HubManagement.Application.Scans.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Scans.Commands
{
    public sealed record ScanHandoverCommand(ScanHandoverRequest Request) : IUpdateCommand<PackageCommandResult>;

    
}
