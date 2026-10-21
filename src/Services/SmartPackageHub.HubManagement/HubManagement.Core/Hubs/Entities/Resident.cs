using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class Resident : EntityBase, IAggregateRoot
    {
        
        public Guid SocietyId { get; private set; }
        public Guid UserId { get; private set; }
        public string? FullName { get; private set; }
        public string? PhoneNumber { get; private set; }
        public string? TowerWing { get; private set; }
        public UnitNumber? Unit { get; private set; } //flat number
        public bool IsMember { get; private set; }
#pragma warning disable CA1056 // Stored as text in the residents table, including relative paths.
        public string? AvatarUrl { get; private set; }
#pragma warning restore CA1056
        public bool IsActive { get; private set; }

        private readonly List<VacationHold> _vacationHolds = new();

        // Expose as read-only collection to encapsulate state modifications
        public IReadOnlyCollection<VacationHold> VacationHolds => _vacationHolds.AsReadOnly();

        private readonly List<Membership> _memberships = new();
        public IReadOnlyCollection<Membership> Memberships => _memberships.AsReadOnly();

        private Resident() { }

        private Resident(Guid societyId, Guid userId, string fullname, string phoneNo, string towerWing, string unitNumber, bool isMember, Uri picUrl, bool isActive)
        {
            this.SocietyId = societyId;
            this.UserId = userId;
            this.FullName = fullname;
            this.PhoneNumber = phoneNo;
            this.TowerWing = towerWing;
            this.Unit = UnitNumber.Create(unitNumber);
            this.IsMember = isMember;
            this.AvatarUrl = picUrl.ToString();
            this.IsActive = isActive;
        }

#pragma warning disable CA1054 // The avatar value is persisted as text and may be a relative path.
        public static Resident Create(Guid societyId, Guid userId, string fullname, string phoneNo, string towerWing, string unitNumber, bool isMember, Uri? picUrl, bool isActive)
        {
            ArgumentNullException.ThrowIfNull(picUrl);
            if (string.IsNullOrWhiteSpace(fullname))
                throw new ArgumentException("Name cannot be empty.", nameof(fullname));
            if (string.IsNullOrWhiteSpace(phoneNo))
                throw new ArgumentException("Mobile number cannot be empty.", nameof(phoneNo));
            if (string.IsNullOrWhiteSpace(towerWing))
                throw new ArgumentException("Tower or wing cannot be empty.", nameof(towerWing));
            if (string.IsNullOrWhiteSpace(unitNumber))
                throw new ArgumentException("Flat number cannot be empty.", nameof(unitNumber));
            return new Resident(societyId, userId, fullname.Trim(), phoneNo.Trim(), towerWing.Trim(), unitNumber, isMember, picUrl, isActive);
        }
#pragma warning restore CA1054
        // Domain Logic: Business rule constraint implementation
        public void ScheduleVacationHold(Guid societyId, Guid holdId, DateTime fromDate, DateTime toDate, bool isActive)
        {
            // Rule: Avoid overlapping vacation holds
            bool hasOverlap = _vacationHolds.Any(h =>
                h.IsActive &&
                (fromDate.Date <= h.ToDate && toDate.Date >= h.FromDate));

            if (hasOverlap)
                throw new InvalidOperationException("Cannot schedule overlapping vacation holds.");

            var hold = VacationHold.Create(societyId, Id, fromDate, toDate, isActive);
            _vacationHolds.Add(hold);
        }

        public bool IsPackageDeliveryPaused(DateTime trackingDate)
        {
            return _vacationHolds.Any(h => h.CoversDate(trackingDate));
        }

        public void ChangeUnit(UnitNumber newUnit)
        {
            Unit = newUnit ?? throw new ArgumentNullException(nameof(newUnit));
        }

        public void AddMembership(Membership membership)
        {
            ArgumentNullException.ThrowIfNull(membership);

            _memberships.Add(membership);
            IsMember = true;
        }

        public bool HasActiveSubscription(DateTime currentTime)
        {
            return _memberships.Any(m => string.Equals(m.Status, "Active", StringComparison.Ordinal) && m.IsMembershipValid(currentTime));
        }
    }
}
