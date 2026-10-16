using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Domains.Abstractions.Entities;
using System.ComponentModel.DataAnnotations;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class Package : EntityBase, IAggregateRoot
    {
        [Required]
        public string PkgNumber { get; private set; } = null!;

        [Required]
        public TrackingNumber SphTrackingNumber { get; private set; } = null!;
        public Guid SocietyId { get; private set; }
        public Guid? ResidentId { get; private set; }
        public Guid? ShelfId { get; private set; }
        public Guid? SecureBagId { get; private set; }
        public Guid? DigitalSignOffReceiptId { get; private set; }
        public Guid StaffUserId { get; private set; }
        public Guid? VacationHoldId { get; private set; }
        public PackageDimensions Dimensions { get; private set; } = null!;
        public string? Category { get; private set; }
        [Required]
        public string CourierAwb { get; private set; } = null!;
        public string? CourierPartner { get; private set; }
        public string RecipientName { get; private set; } = null!;
        //public string? ContactPhone { get; private set; }
        public string TowerWing { get; private set; } = null!;
        public UnitNumber FlatNumber { get; private set; } = null!;
        public PackageStatus Status { get; private set; }
        public bool IsColdStorage { get; private set; }
        //public bool IsDamaged { get; private set; }
        public bool IsCod { get; private set; }
        public decimal? CodAmount { get; private set; }
        public PackageIntake PackageIntake { get; private set; } = null!;
        public DateTime RecievedAt { get; private set; }

        private readonly List<PackageImage> _images = new();
        public IReadOnlyCollection<PackageImage> Images => _images.AsReadOnly();

        private Package() { }
        private Package(
            Guid societyId,
            Guid? residentId,
            Guid? shelfId,
            Guid staffUserId,
            Guid? vacationHoldId,
            string pkgNumber,
            string trackingNumber,
            PackageDimensions dimensions,
            string courierPartnerName,
            string courierAwb,
            string recipientName,
            //string? contactPhone,
            string towerWing,
            string flatNumber,
            PackageStatus status,
            bool isColdStorage,
            //bool isDamaged,
            bool isCod,
            decimal? codAmount,
            DateTime receivedAt,
            Guid? secureBagId = null,
            Guid? digitalSignOffReceiptId = null,
            string? category = null)
        {
            this.SocietyId = societyId;
            this.ResidentId = residentId;
            this.ShelfId = shelfId;
            this.SecureBagId = secureBagId;
            this.DigitalSignOffReceiptId = digitalSignOffReceiptId;
            this.StaffUserId = staffUserId;
            this.VacationHoldId = vacationHoldId;
            this.PkgNumber = pkgNumber;
            this.SphTrackingNumber = TrackingNumber.Create(trackingNumber);
            this.Dimensions = dimensions;
            this.CourierPartner = courierPartnerName;
            this.CourierAwb = courierAwb;
            this.Category = category;
            this.RecipientName = recipientName;
            //this.ContactPhone = contactPhone;
            this.TowerWing = towerWing;
            this.FlatNumber = UnitNumber.Create(flatNumber);
            this.Status = status;
            this.IsColdStorage = isColdStorage;
            //this.IsDamaged = isDamaged;
            this.IsCod = isCod;
            this.CodAmount = codAmount;
            this.RecievedAt = receivedAt;

            // Generate the intake child entity implicitly upon creation
            PackageIntake = PackageIntake.Create(Id, staffUserId, DateTime.UtcNow, "missing", string.Empty, 5, string.Empty);
        }

        public static Package Create(
            Guid societyId,
            Guid? residentId,
            Guid? shelfId,
            Guid staffUserId,
            string trackingNumber,
            PackageDimensions dimensions,
            string courierPartnerName,
            string courierAwb,
            string recipientName,
            string towerWing,
            string flatNumber,
            PackageStatus status,
            DateTime receivedAt,
            //string? contactPhone = null,
            bool isColdStorage = false,
            //bool isDamaged = false,
            bool isCod = false,
            decimal? codAmount = null,
            Guid? vacationHoldId = null,
            string? pkgNumber = null,
            Guid? secureBagId = null,
            Guid? digitalSignOffReceiptId = null,
            string? category = null)
        {
            var resolvedPkgNumber = string.IsNullOrWhiteSpace(pkgNumber) ? trackingNumber : pkgNumber;
            var package = new Package(
                societyId,
                residentId,
                shelfId,
                staffUserId,
                vacationHoldId,
                resolvedPkgNumber,
                trackingNumber,
                dimensions,
                courierPartnerName,
                courierAwb,
                recipientName,
               // contactPhone,
                towerWing,
                flatNumber,
                status,
                isColdStorage,
               // isDamaged,
                isCod,
                codAmount,
                receivedAt,
                secureBagId,
                digitalSignOffReceiptId,
                category);
            return package;
        }

        public void AssignShelf(Guid shelfId)
        {
            ShelfId = shelfId;
        }

        public void AssignSecureBag(Guid secureBagId)
        {
            SecureBagId = secureBagId;
        }

        public void LinkDigitalSignOffReceipt(Guid receiptId)
        {
            DigitalSignOffReceiptId = receiptId;
        }

        public void LinkResident(Guid residentId)
        {
            ResidentId = residentId;
        }

        public void ApplyVacationHold(Guid holdId)
        {
            VacationHoldId = holdId;
        }

       // public void MarkAsDamaged()
        //{
            //IsDamaged = true;
        //}

        // Domain Behavior: Adding an image to the tracking package
        public void AttachEvidenceImage(string kind = "Evidence")
        {
            var image = PackageImage.Create(Id, PackageIntake.Id, kind, string.Empty, string.Empty, DateTime.UtcNow);
            _images.Add(image);
        }

        // Domain Behavior: Managing state transitions strictly through business rules
        public void ShelfPackage()
        {
            if (Status != PackageStatus.Received)
                throw new InvalidOperationException("Only received packages can be shelved.");

            Status = PackageStatus.Shelved;
        }

        public void DispatchPackage()
        {
            if (Status != PackageStatus.Shelved)
                throw new InvalidOperationException("Packages must be shelved before being dispatched.");

            Status = PackageStatus.Dispatched;
        }

        public void DeliverPackage()
        {
            if (Status != PackageStatus.Dispatched)
                throw new InvalidOperationException("Packages must be dispatched before they can be marked delivered.");

            Status = PackageStatus.Delivered;
        }

        public void MarkCollected()
        {
            if (Status == PackageStatus.Delivered)
                return;

            Status = PackageStatus.Delivered;
        }
    }
}
