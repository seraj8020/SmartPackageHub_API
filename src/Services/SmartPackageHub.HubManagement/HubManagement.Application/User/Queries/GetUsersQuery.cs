using AutoMapper;
using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubUsers.Queries
{
    public sealed record GetUsersQuery : Query<List<UserDetailsDto>>;

    public sealed class GetUsersQueryHandler : QueryHandler<GetUsersQuery, List<UserDetailsDto>>
    {
        private readonly IRepository<User> _users;

        public GetUsersQueryHandler(IMapper mapper, IRepository<User> users) : base(mapper)
        {
            _users = users;
        }

        protected override Task<List<UserDetailsDto>> HandleAsync(GetUsersQuery request)
        {
            var users = _users.GetAll()
                .OrderBy(u => u.Username)
                .ToList()
                .Select(UserDetailsDto.From)
                .ToList();

            return Task.FromResult(users);
        }
    }

   
}
