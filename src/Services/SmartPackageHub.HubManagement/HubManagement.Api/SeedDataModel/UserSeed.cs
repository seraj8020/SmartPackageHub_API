namespace HubManagement.Api.SeedDataModel
{
    internal sealed class UserSeed
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = "Resident";
        public string Status { get; set; } = "Active";
    }
}
