using HubManagement.Application.HubUsers.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.HubUsers.Queries
{
    public sealed record GetUserByIdQuery(Guid UserId) : Query<UserDetailsDto?>;

    
}
