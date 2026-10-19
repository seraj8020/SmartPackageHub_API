using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class VacationHold : EntityBase
    {
        public Guid SocietyId { get; private set; }
        public Guid ResidentId { get; private set; }
        public DateTime FromDate { get; private set; }
        public DateTime ToDate { get; private set; }
        public bool IsActive { get; private set; }
        
        private VacationHold(Guid societyId, Guid residentId, DateTime fromDate, DateTime toDate, bool isActive)
        {
            if (fromDate.Date < DateTime.UtcNow.Date)
                throw new ArgumentException("Vacation hold cannot start in the past.", nameof(fromDate));

            if (toDate <= fromDate)
                throw new ArgumentException("End date must be after the start date.", nameof(toDate));
            this.SocietyId = societyId;
            this.ResidentId = residentId;
            this.FromDate = fromDate.Date;
            this.ToDate = toDate.Date;
            this.IsActive = true;

        }
        public static VacationHold Create(Guid societyId, Guid residentId, DateTime fromDate, DateTime toDate, bool isActive)
        {
            return new VacationHold(societyId, residentId, fromDate, toDate, isActive);
        }

        public bool CoversDate(DateTime date)
        {
            return IsActive && date.Date >= FromDate && date.Date <= ToDate;
        }

        public void Cancel()
        {
            if (!IsActive)
                throw new InvalidOperationException("Vacation hold is already inactive.");

            IsActive = false;
        }
    }
}
