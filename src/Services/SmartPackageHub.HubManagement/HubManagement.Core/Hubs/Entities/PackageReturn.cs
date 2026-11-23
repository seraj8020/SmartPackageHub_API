using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Domains.Abstractions.Entities;
using System.ComponentModel.DataAnnotations;

namespace HubManagement.Core.Hubs.Entities
{
    /// <summary>
    /// Resident return request for a delivered package (separate from inbound intake damage).
    /// </summary>
    public sealed class PackageReturn : EntityBase, IAggregateRoot
    {
        public const int NoteMaxLength = 240;
        public const int MaxPhotos = 3;

        public Guid PackageId { get; private set; }
        public Guid SocietyId { get; private set; }
        public Guid ResidentId { get; private set; }

        public PackageReturnStatus Status { get; private set; }
        public PackageReturnReason Reason { get; private set; }

        [MaxLength(NoteMaxLength)]
        public string? Note { get; private set; }

        public DateTime RequestedAtUtc { get; private set; }

        public DateTime? ReceivedAtUtc { get; private set; }
        public Guid? ReceivedByStaffUserId { get; private set; }

        /// <summary>Shelf used while the return is held at the hub (return lifecycle).</summary>
        public Guid? ShelfId { get; private set; }
        public DateTime? ShelfAssignedAtUtc { get; private set; }

        [MaxLength(500)]
        public string? CourierPickupNote { get; private set; }

        [MaxLength(100)]
        public string? CourierPickupAwb { get; private set; }

        public DateTime? ClosedAtUtc { get; private set; }

        private readonly List<PackageReturnImage> _images = new();
        public IReadOnlyCollection<PackageReturnImage> Images => _images.AsReadOnly();

        private PackageReturn() { }

        private PackageReturn(
            Guid packageId,
            Guid societyId,
            Guid residentId,
            PackageReturnReason reason,
            string? note,
            DateTime requestedAtUtc)
        {
            PackageId = packageId;
            SocietyId = societyId;
            ResidentId = residentId;
            Reason = reason;
            Note = NormalizeNote(note);
            RequestedAtUtc = requestedAtUtc;
            Status = PackageReturnStatus.ReturnRequested;
        }

        public static PackageReturn Create(
            Guid packageId,
            Guid societyId,
            Guid residentId,
            PackageReturnReason reason,
            string? note,
            DateTime? requestedAtUtc = null)
        {
            if (packageId == Guid.Empty)
                throw new ArgumentException("Package id is required.", nameof(packageId));
            if (societyId == Guid.Empty)
                throw new ArgumentException("Society id is required.", nameof(societyId));
            if (residentId == Guid.Empty)
                throw new ArgumentException("Resident id is required.", nameof(residentId));

            if (reason == PackageReturnReason.Other && string.IsNullOrWhiteSpace(note))
                throw new ArgumentException("A note is required when return reason is Other.", nameof(note));
            if (note is { Length: > NoteMaxLength })
                throw new ArgumentException($"Note must be at most {NoteMaxLength} characters.", nameof(note));

            return new PackageReturn(
                packageId,
                societyId,
                residentId,
                reason,
                note,
                requestedAtUtc ?? DateTime.UtcNow);
        }

        public void MarkReceived(Guid staffUserId, DateTime? receivedAtUtc = null)
        {
            if (Status != PackageReturnStatus.ReturnRequested)
                throw new InvalidOperationException("Only ReturnRequested returns can be marked received.");
            if (staffUserId == Guid.Empty)
                throw new ArgumentException("Staff user id is required.", nameof(staffUserId));

            Status = PackageReturnStatus.ReturnReceived;
            ReceivedByStaffUserId = staffUserId;
            ReceivedAtUtc = receivedAtUtc ?? DateTime.UtcNow;
        }

        public void AssignReturnShelf(Guid shelfId, DateTime? assignedAtUtc = null)
        {
            if (Status != PackageReturnStatus.ReturnReceived && Status != PackageReturnStatus.ReturnShelved)
                throw new InvalidOperationException("Return must be received before assigning a return shelf.");
            if (shelfId == Guid.Empty)
                throw new ArgumentException("Shelf id is required.", nameof(shelfId));

            ShelfId = shelfId;
            ShelfAssignedAtUtc = assignedAtUtc ?? DateTime.UtcNow;
            Status = PackageReturnStatus.ReturnShelved;
        }

        public void MarkCourierPickedUp(string? note = null, string? awb = null, DateTime? closedAtUtc = null)
        {
            if (Status != PackageReturnStatus.ReturnShelved)
                throw new InvalidOperationException("Only ReturnShelved returns can be marked picked by courier.");

            CourierPickupNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            CourierPickupAwb = string.IsNullOrWhiteSpace(awb) ? null : awb.Trim();
            if (CourierPickupNote?.Length > 500)
                throw new ArgumentException("Courier pickup note is too long.", nameof(note));
            if (CourierPickupAwb?.Length > 100)
                throw new ArgumentException("Courier pickup AWB is too long.", nameof(awb));

            Status = PackageReturnStatus.ReturnPickedByCourier;
            ClosedAtUtc = closedAtUtc ?? DateTime.UtcNow;
        }

        public void Close(DateTime? closedAtUtc = null)
        {
            if (Status is PackageReturnStatus.ReturnPickedByCourier or PackageReturnStatus.ReturnClosed)
                throw new InvalidOperationException("Return is already closed.");

            Status = PackageReturnStatus.ReturnClosed;
            ClosedAtUtc = closedAtUtc ?? DateTime.UtcNow;
        }

        public PackageReturnImage AddPhoto(
            string storageKey,
            string sha256Hash,
            string contentType,
            long byteSize,
            int sortOrder,
            DateTime? capturedAtUtc = null)
        {
            if (_images.Count >= MaxPhotos)
                throw new InvalidOperationException($"A return supports at most {MaxPhotos} photos.");
            if (sortOrder < 0 || sortOrder > 2)
                throw new ArgumentOutOfRangeException(nameof(sortOrder), "Photo sort order must be 0–2.");

            var image = PackageReturnImage.Create(
                Id,
                storageKey,
                sha256Hash,
                contentType,
                byteSize,
                sortOrder,
                capturedAtUtc ?? DateTime.UtcNow);
            _images.Add(image);
            return image;
        }

        public bool IsOpen =>
            Status is PackageReturnStatus.ReturnRequested
                or PackageReturnStatus.ReturnReceived
                or PackageReturnStatus.ReturnShelved;

        private static string? NormalizeNote(string? note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return null;
            var trimmed = note.Trim();
            return trimmed.Length > NoteMaxLength ? trimmed[..NoteMaxLength] : trimmed;
        }
    }
}
