using AutoMapper;
using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubUsers.Queries
{
    public sealed class GetUserByIdQueryHandler : QueryHandler<GetUserByIdQuery, UserDetailsDto?>
    {
        private readonly IRepository<User> _users;

        public GetUserByIdQueryHandler(IMapper mapper, IRepository<User> users) : base(mapper)
        {
            _users = users;
        }

        protected override async Task<UserDetailsDto?> HandleAsync(GetUserByIdQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var user = await _users.GetByIdAsync(request.UserId).ConfigureAwait(false);
            return user == null ? null : UserDetailsDto.From(user);
        }
    }
}
