using HubManagement.Application.PackageHub.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed record RespondCodCommand(Guid PackageId, bool Approve) : ICreateCommand<CodResponseResult>;
}
