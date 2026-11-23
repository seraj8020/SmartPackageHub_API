using AutoMapper;
using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubUsers.Queries
{
    public sealed class GetAdminUsersQueryHandler : QueryHandler<GetAdminUsersQuery, GetAdminUsersQueryResult>
    {
        private readonly IRepository<User> _users;
        private readonly IRepository<UserSociety> _userSocieties;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<Resident> _residents;

        public GetAdminUsersQueryHandler(
            IMapper mapper,
            IRepository<User> users,
            IRepository<UserSociety> userSocieties,
            IRepository<Society> societies,
            IRepository<Resident> residents) : base(mapper)
        {
            _users = users;
            _userSocieties = userSocieties;
            _societies = societies;
            _residents = residents;
        }

        protected override Task<GetAdminUsersQueryResult> HandleAsync(GetAdminUsersQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!string.Equals(request.CallerRole, "PlatformAdmin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.CallerRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new GetAdminUsersQueryResult(true, false, "Admin access required.", Array.Empty<UserDetailsDto>()));
            }

            var isPlatformAdmin = string.Equals(request.CallerRole, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            Guid? societyId = null;
            if (!isPlatformAdmin)
            {
                societyId = request.CallerSocietyIds.Count > 0
                    ? request.CallerSocietyIds[0]
                    : Guid.Empty;
            }
            if (!isPlatformAdmin && societyId == Guid.Empty)
            {
                return Task.FromResult(new GetAdminUsersQueryResult(true, false, "No society is linked to this admin.", Array.Empty<UserDetailsDto>()));
            }

            var links = _userSocieties.GetAll().AsEnumerable().ToList();
            var societyNames = _societies.GetAll().AsEnumerable().ToDictionary(society => society.Id, society => society.Name);
            var users = _users.GetAll()
                .OrderBy(user => user.Username)
                .AsEnumerable()
                .Select(user =>
                {
                    var dto = UserDetailsDto.From(user);
                    dto.SocietyIds = links.Where(link => link.UserId == user.Id).Select(link => link.SocietyId).Distinct().ToList();
                    dto.SocietyNames = dto.SocietyIds.Where(societyNames.ContainsKey).Select(id => societyNames[id]).ToList();
                    return dto;
                })
                .ToList();

            var residentRows = BuildResidentRows(societyId, isPlatformAdmin, societyNames);

            if (request.ResidentsOnly)
                return Task.FromResult(new GetAdminUsersQueryResult(false, true, string.Empty, residentRows));

            if (!isPlatformAdmin)
                users = users.Where(user => user.SocietyIds.Contains(societyId!.Value)).ToList();

            var residentUserIds = _residents.GetAll().AsEnumerable().Select(resident => resident.UserId).ToHashSet();
            var staff = users.Where(user => !residentUserIds.Contains(user.Id)).ToList();
            return Task.FromResult(new GetAdminUsersQueryResult(false, true, string.Empty, staff));
        }

        private List<UserDetailsDto> BuildResidentRows(Guid? societyId, bool isPlatformAdmin, Dictionary<Guid, string> societyNames)
        {
            var residents = _residents.GetAll().AsEnumerable();
            if (!isPlatformAdmin)
                residents = residents.Where(resident => resident.SocietyId == societyId);

            var userMap = _users.GetAll().AsEnumerable().ToDictionary(user => user.Id, user => user);
            return residents
                .Where(resident => userMap.ContainsKey(resident.UserId))
                .Select(resident =>
                {
                    var user = userMap[resident.UserId];
                    societyNames.TryGetValue(resident.SocietyId, out var name);
                    return new UserDetailsDto
                    {
                        Id = resident.UserId,
                        Username = resident.FullName ?? user.Username,
                        Email = user.ContactInfo?.Email ?? string.Empty,
                        PhoneNumber = resident.PhoneNumber,
                        Role = "Resident",
                        Status = resident.IsActive ? "Active" : "Not Active",
                        SocietyIds = new[] { resident.SocietyId },
                        SocietyNames = name is null
                            ? Array.Empty<string>()
                            : new[] { name },
                        TowerWing = resident.TowerWing,
                        UnitNumber = resident.Unit?.Value ?? string.Empty,
                        IsMember = resident.IsMember
                    };
                })
                .ToList();
        }
    }
}