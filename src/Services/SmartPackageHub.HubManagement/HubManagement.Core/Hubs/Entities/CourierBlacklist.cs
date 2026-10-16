using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class CourierBlacklist : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; private set; }
        public string CarrierName { get; private set; }
        public string? AgentName { get; private set; }
        public string Phone { get; private set; }
        public string Reason { get; private set; }
        public DateTime BlacklistedAt { get; private set; }
        public bool IsActive { get; private set; }

        private CourierBlacklist(Guid societyId, string carrierName, string? agentName, string phone, string reason)
        {
            SocietyId = societyId;
            CarrierName = carrierName;
            AgentName = agentName;
            Phone = phone;
            Reason = reason;
            BlacklistedAt = DateTime.UtcNow;
            IsActive = true;
        }

        public static CourierBlacklist Create(Guid societyId, string carrierName, string? agentName, string phone, string reason)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Phone number is required.", nameof(phone));
            return new CourierBlacklist(societyId, carrierName, agentName, phone, reason);
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
