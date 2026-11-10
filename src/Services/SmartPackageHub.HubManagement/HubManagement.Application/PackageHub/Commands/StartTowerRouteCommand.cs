using HubManagement.Application.PackageHub.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed record StartTowerRouteCommand(
        Guid SocietyId,
        string TowerWing,
        int? Floor = null) : ICreateCommand<StartTowerRouteResult>;
}
