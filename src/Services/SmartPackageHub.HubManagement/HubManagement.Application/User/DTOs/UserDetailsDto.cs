using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;

namespace HubManagement.Application.HubUsers.DTOs
{
    public sealed class UserDetailsDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public string Status { get; set; } = string.Empty;

        public static UserDetailsDto From(User user)
        {
            var role = user.Role.Type;
            return new UserDetailsDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.ContactInfo?.Email ?? string.Empty,
                Role = role.ToString(),
                IsAdmin = role == RoleType.Admin,
                Status = user.Status
            };
        }
    }

    public sealed record UserCommandResult(bool Found, bool Conflict, bool Succeeded, string Message, UserDetailsDto? User);
}
