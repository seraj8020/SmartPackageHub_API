using SmartPackageHub.Domains.Abstractions.Models;

namespace HubManagement.Application.Societies.DTOs
{
    public sealed class CreateSocietyRequest
    {
        public string Name { get; set; } = string.Empty;
        public AddressData? Address { get; set; }
        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
    }
}
