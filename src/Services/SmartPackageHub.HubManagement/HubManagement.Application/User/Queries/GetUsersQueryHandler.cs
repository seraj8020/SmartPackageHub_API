using AutoMapper;
using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Application.HubUsers.Queries;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubUsers.Queries
{
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
                .AsEnumerable() // Switches to in-memory processing for the DTO projection
                .Select(UserDetailsDto.From)
                .ToList();

            return Task.FromResult(users);
        }
    }
}
