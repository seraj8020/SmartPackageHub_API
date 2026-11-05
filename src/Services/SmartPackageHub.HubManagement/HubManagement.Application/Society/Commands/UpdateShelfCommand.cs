using HubManagement.Application.Societies.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Societies.Commands
{
    public sealed record UpdateShelfCommand(
        Guid SocietyId,
        Guid ShelfId,
        string Code,
        string TowerOrWing,
        bool IsColdStorage,
        int Capacity,
        string Zone,
        string Aisle,
        int Tier) : IUpdateCommand<ShelfCommandResult>;
}
