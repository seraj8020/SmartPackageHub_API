using HubManagement.Application.Societies.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Societies.Commands
{
    public sealed record CreateShelfCommand(
        Guid SocietyId,
        string Code,
        string TowerOrWing,
        bool IsColdStorage,
        int Capacity,
        string Zone,
        string Aisle,
        int Tier) : ICreateCommand<ShelfCommandResult>;
}
