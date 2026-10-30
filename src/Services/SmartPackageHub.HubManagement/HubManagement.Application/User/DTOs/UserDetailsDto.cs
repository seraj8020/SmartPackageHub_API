using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;

namespace HubManagement.Application.HubUsers.DTOs
{
    public sealed class UserDetailsDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public string Status { get; set; } = string.Empty;

        public static UserDetailsDto From(User user)
        {
            ArgumentNullException.ThrowIfNull(user);
            var role = user.Role.Type;
            return new UserDetailsDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.ContactInfo?.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = role.ToString(),
                IsAdmin = role == RoleType.Admin,
                Status = user.Status
            };
        }
    }

    
}
