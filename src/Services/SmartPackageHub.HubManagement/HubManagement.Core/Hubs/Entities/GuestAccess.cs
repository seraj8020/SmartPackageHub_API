using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class GuestAccess : EntityBase, IAggregateRoot
    {
        public Guid ResidentId { get; private set; }
        public Guid SocietyId { get; private set; }
        public string GuestName { get; private set; } = null!;
        public AccessWindow Window { get; private set; } = null!;
        //public AccessCode Code { get; private set; }
        public string? PhoneNo { get; private set; }

        private GuestAccess() { }
        private GuestAccess(Guid residentId, Guid societyId, string guestName, string phoneNo, AccessWindow window)
        {
            //GuestId = Id;
            ResidentId = residentId;
            SocietyId = societyId;
            GuestName = guestName;
            PhoneNo = phoneNo;
            Window = window;
        }

        public static GuestAccess Create(Guid residentId, Guid societyId, string guestName, string phoneNo, DateTime validFrom, DateTime validTo)
        {
            var acceessWindow = AccessWindow.Create(validFrom, validTo);
            var guestAccess = new GuestAccess(residentId, societyId, guestName, phoneNo, acceessWindow);
            return guestAccess;
        }


    }
}
