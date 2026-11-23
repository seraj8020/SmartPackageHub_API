using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    /// <summary>
    /// Carrier-brand return window for a society (e.g. Amazon 7 days, DTDC 3 days).
    /// Matches <see cref="Package.CourierPartner"/> by carrier name — not <see cref="CourierAgent"/>
    /// (agents are people; return policy belongs to the courier brand).
    /// </summary>
    public sealed class SocietyCarrierReturnPolicy : EntityBase, IAggregateRoot
    {
        public const int DefaultReturnWindowDays = 7;

        public Guid SocietyId { get; private set; }

        /// <summary>Courier brand name aligned with Package.CourierPartner (Amazon, Flipkart, DTDC, …).</summary>
        public string CarrierName { get; private set; } = null!;

        public int ReturnWindowDays { get; private set; }

        private SocietyCarrierReturnPolicy() { }

        private SocietyCarrierReturnPolicy(Guid societyId, string carrierName, int returnWindowDays)
        {
            SocietyId = societyId;
            CarrierName = carrierName;
            ReturnWindowDays = returnWindowDays;
        }

        public static SocietyCarrierReturnPolicy Create(
            Guid societyId,
            string carrierName,
            int returnWindowDays = DefaultReturnWindowDays)
        {
            if (societyId == Guid.Empty)
                throw new ArgumentException("Society id is required.", nameof(societyId));
            if (string.IsNullOrWhiteSpace(carrierName))
                throw new ArgumentException("Carrier name is required.", nameof(carrierName));
            if (returnWindowDays < 1 || returnWindowDays > 90)
                throw new ArgumentOutOfRangeException(nameof(returnWindowDays), "Return window must be 1–90 days.");

            return new SocietyCarrierReturnPolicy(societyId, carrierName.Trim(), returnWindowDays);
        }

        public void ChangeReturnWindowDays(int returnWindowDays)
        {
            if (returnWindowDays < 1 || returnWindowDays > 90)
                throw new ArgumentOutOfRangeException(nameof(returnWindowDays), "Return window must be 1–90 days.");

            ReturnWindowDays = returnWindowDays;
        }

        public void RenameCarrier(string carrierName)
        {
            if (string.IsNullOrWhiteSpace(carrierName))
                throw new ArgumentException("Carrier name is required.", nameof(carrierName));

            CarrierName = carrierName.Trim();
        }
    }
}
