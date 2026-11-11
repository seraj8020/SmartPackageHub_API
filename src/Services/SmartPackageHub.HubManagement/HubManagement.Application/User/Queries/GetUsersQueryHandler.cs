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
        private readonly IRepository<UserSociety> _userSocieties;
        private readonly IRepository<Society> _societies;

        public GetUsersQueryHandler(IMapper mapper, IRepository<User> users, IRepository<UserSociety> userSocieties, IRepository<Society> societies) : base(mapper)
        {
            _users = users;
            _userSocieties = userSocieties;
            _societies = societies;
        }
       
        protected override Task<List<UserDetailsDto>> HandleAsync(GetUsersQuery request)
        {
            var links = _userSocieties.GetAll().AsEnumerable().ToList();
            var names = _societies.GetAll().AsEnumerable().ToDictionary(s => s.Id, s => s.Name);
            var users = _users.GetAll()
                .OrderBy(u => u.Username)
                .AsEnumerable() // Switches to in-memory processing for the DTO projection
                .Select(user =>
                {
                    var dto = UserDetailsDto.From(user);
                    dto.SocietyIds = links.Where(link => link.UserId == user.Id).Select(link => link.SocietyId).Distinct().ToList();
                    dto.SocietyNames = dto.SocietyIds.Where(names.ContainsKey).Select(id => names[id]).ToList();
                    return dto;
                })
                .ToList();

            return Task.FromResult(users);
        }
    }
}
