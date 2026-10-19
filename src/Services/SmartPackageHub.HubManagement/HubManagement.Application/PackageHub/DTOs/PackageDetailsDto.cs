using HubManagement.Core.Hubs.Entities;

namespace HubManagement.Application.PackageHub.DTOs
{
    

    public sealed class PackageDetailsDto
    {
        public Guid Id { get; set; }
        public string PkgNumber { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ResidentId { get; set; }
        public string? Courier { get; set; }
        public DateTime ReceivedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? ShelfId { get; set; }
        public string TowerWing { get; set; } = string.Empty;
        public string FlatNumber { get; set; } = string.Empty;

        public static PackageDetailsDto From(Package package) => new()
        {
            Id = package.Id,
            PkgNumber = package.PkgNumber,
            TrackingNumber = package.SphTrackingNumber.Value,
            Description = package.Category,
            ResidentId = package.ResidentId,
            Courier = package.CourierPartner,
            ReceivedAt = package.RecievedAt,
            Status = package.Status.ToString(),
            ShelfId = package.ShelfId,
            TowerWing = package.TowerWing,
            FlatNumber = package.FlatNumber.Value
        };
    }

    public sealed record PackageCommandResult(bool Found, bool Conflict, bool Succeeded, string Message, PackageDetailsDto? Package);
}
