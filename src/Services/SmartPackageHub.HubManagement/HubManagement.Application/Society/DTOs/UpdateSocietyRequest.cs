using SmartPackageHub.Domains.Abstractions.Models;

namespace HubManagement.Application.Societies.DTOs
{
    public sealed class UpdateSocietyRequest
    {
        public string? Name { get; set; }
        public AddressData? Address { get; set; }
        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
    }
}
