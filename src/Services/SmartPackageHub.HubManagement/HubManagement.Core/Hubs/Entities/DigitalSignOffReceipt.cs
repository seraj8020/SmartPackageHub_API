using SmartPackageHub.Domains.Abstractions.Entities;
using System.ComponentModel.DataAnnotations;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class DigitalSignOffReceipt : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; private set; }

        [Required]
        public string ReceiptNumber { get; private set; } = null!; // Unique receipt identifier e.g. "DSR-20261015-0001"

        [Required]
        public string CourierPartner { get; private set; } = null!; // Amazon, Flipkart, DTDC, etc.
        public Guid? CourierAgentId { get; private set; }
        public string CourierAgentName { get; private set; } = null!;
        public string CourierAgentPhone { get; private set; } = null!;
        public Guid StaffUserId { get; private set; }
        public int TotalPackagesCount { get; private set; }
        public string? DigitalSignature { get; private set; }
        public string Status { get; private set; } = "Issued"; // Issued, Signed, Acknowledged
        public DateTime SignedAt { get; private set; }
        public string? Notes { get; private set; }

        private readonly List<Package> _packages = new();
        public IReadOnlyCollection<Package> Packages => _packages.AsReadOnly();

        private DigitalSignOffReceipt() { }

        private DigitalSignOffReceipt(
            Guid societyId,
            string receiptNumber,
            string courierPartner,
            Guid? courierAgentId,
            string courierAgentName,
            string courierAgentPhone,
            Guid staffUserId,
            int totalPackagesCount,
            string? digitalSignature,
            string status,
            DateTime signedAt,
            string? notes)
        {
            SocietyId = societyId;
            ReceiptNumber = receiptNumber;
            CourierPartner = courierPartner;
            CourierAgentId = courierAgentId;
            CourierAgentName = courierAgentName;
            CourierAgentPhone = courierAgentPhone;
            StaffUserId = staffUserId;
            TotalPackagesCount = totalPackagesCount;
            DigitalSignature = digitalSignature;
            Status = status;
            SignedAt = signedAt;
            Notes = notes;
        }

        public static DigitalSignOffReceipt Create(
            Guid societyId,
            string receiptNumber,
            string courierPartner,
            string courierAgentName,
            string courierAgentPhone,
            Guid staffUserId,
            int totalPackagesCount,
            Guid? courierAgentId = null,
            string? digitalSignature = null,
            string status = "Issued",
            DateTime? signedAt = null,
            string? notes = null)
        {
            if (string.IsNullOrWhiteSpace(receiptNumber))
                throw new ArgumentException("Receipt number cannot be empty.", nameof(receiptNumber));
            if (string.IsNullOrWhiteSpace(courierPartner))
                throw new ArgumentException("Courier partner cannot be empty.", nameof(courierPartner));
            if (string.IsNullOrWhiteSpace(courierAgentName))
                throw new ArgumentException("Courier agent name cannot be empty.", nameof(courierAgentName));
            if (string.IsNullOrWhiteSpace(courierAgentPhone))
                throw new ArgumentException("Courier agent phone cannot be empty.", nameof(courierAgentPhone));
            if (staffUserId == Guid.Empty)
                throw new ArgumentException("Staff user id is required.", nameof(staffUserId));

            return new DigitalSignOffReceipt(
                societyId,
                receiptNumber.Trim(),
                courierPartner.Trim(),
                courierAgentId,
                courierAgentName.Trim(),
                courierAgentPhone.Trim(),
                staffUserId,
                totalPackagesCount,
                digitalSignature,
                status,
                signedAt ?? DateTime.UtcNow,
                notes?.Trim());
        }

        public void AcknowledgeReceipt(string digitalSignature)
        {
            if (string.IsNullOrWhiteSpace(digitalSignature))
                throw new ArgumentException("Digital signature cannot be empty.", nameof(digitalSignature));

            DigitalSignature = digitalSignature;
            Status = "Acknowledged";
        }

        public void AddPackage(Package package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));

            _packages.Add(package);
            TotalPackagesCount = _packages.Count;
        }
    }
}
