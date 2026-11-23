using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubUsers.Services
{
    internal static class UserDetailsDtoMapper
    {
        public static UserDetailsDto FromUser(
            User user,
            IRepository<UserSociety> userSocieties,
            IRepository<Society> societies)
        {
            var dto = UserDetailsDto.From(user);
            var links = userSocieties.GetAll().AsEnumerable().Where(link => link.UserId == user.Id).ToList();
            if (links.Count > 0)
            {
                dto.SocietyIds = links.Select(link => link.SocietyId).Distinct().ToList();
            }

            EnrichSocietyNames(dto, societies);
            return dto;
        }

        public static void EnrichSocietyNames(UserDetailsDto dto, IRepository<Society> societies)
        {
            if (dto.SocietyIds.Count == 0)
            {
                dto.SocietyNames = Array.Empty<string>();
                return;
            }

            var names = societies.GetAll().AsEnumerable()
                .Where(society => dto.SocietyIds.Contains(society.Id))
                .ToDictionary(society => society.Id, society => society.Name);
            dto.SocietyNames = dto.SocietyIds.Where(names.ContainsKey).Select(id => names[id]).ToList();
        }
    }
}