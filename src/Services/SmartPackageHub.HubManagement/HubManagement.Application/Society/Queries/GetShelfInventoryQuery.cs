using HubManagement.Application.Societies.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Societies.Queries
{
    public sealed record GetShelfInventoryQuery(Guid SocietyId) : Query<List<ShelfInventoryDto>?>;
}
