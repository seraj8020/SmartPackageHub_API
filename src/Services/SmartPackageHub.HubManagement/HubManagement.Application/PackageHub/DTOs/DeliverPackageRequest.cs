using HubManagement.Core.Hubs.Models;

namespace HubManagement.Application.PackageHub.DTOs
{
    public class DeliverPackageRequest
    {
        public Guid SocietyId { get; set; }
        public Guid StaffUserId { get; set; }
        public Guid ResidentId { get; set; }
        public string? Token { get; set; }
        public Guid? BatchId { get; set; }
        public PackageStatus Status { get; set; }
        public string? Method { get; set; }
        public string? Notes { get; set; }
    }
}
