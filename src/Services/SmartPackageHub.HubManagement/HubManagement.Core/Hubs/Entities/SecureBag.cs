using SmartPackageHub.Domains.Abstractions.Entities;
using System.ComponentModel.DataAnnotations;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class SecureBag : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; private set; }

        [Required]
        public string BagNumber { get; private set; } = null!; // Unique Secure Hub Bag ID within society e.g. "SHB-BAG-1001"
        public string? Barcode { get; private set; }
        public string? QrCode { get; private set; }
        public string? SealNumber { get; private set; }
        public string Status { get; private set; } = "Available"; // Available, Assigned, InTransit, Maintenance, Sealed
        public string? CapacityOrSize { get; private set; } // Small, Medium, Large
        public bool IsActive { get; private set; } = true;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        private readonly List<Package> _packages = new();
        public IReadOnlyCollection<Package> Packages => _packages.AsReadOnly();

        private SecureBag() { }

        private SecureBag(Guid societyId, string bagNumber, string? barcode, string? qrCode, string? sealNumber, string? capacityOrSize, string status)
        {
            SocietyId = societyId;
            BagNumber = bagNumber;
            Barcode = barcode;
            QrCode = qrCode;
            SealNumber = sealNumber;
            CapacityOrSize = capacityOrSize;
            Status = status;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public static SecureBag Create(Guid societyId, string bagNumber, string? barcode = null, string? qrCode = null, string? sealNumber = null, string? capacityOrSize = null, string status = "Available")
        {
            if (string.IsNullOrWhiteSpace(bagNumber))
                throw new ArgumentException("Bag number cannot be empty.", nameof(bagNumber));

            return new SecureBag(societyId, bagNumber.Trim(), barcode?.Trim(), qrCode?.Trim(), sealNumber?.Trim(), capacityOrSize?.Trim(), status);
        }

        public void MarkAssigned(string? sealNumber = null)
        {
            Status = "Assigned";
            if (!string.IsNullOrWhiteSpace(sealNumber))
            {
                SealNumber = sealNumber.Trim();
            }
        }

        public void MarkAvailable()
        {
            Status = "Available";
            SealNumber = null;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }
    }
}
