using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class DispatchBatch : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; private set; }
        public Guid RunnerUserId { get; private set; }
        public string TowerWing { get; private set; } = null!;
        public int? Floor { get; }
        public UnitNumber FlatNumber { get; private set; } = null!;
        public string? Status { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private DispatchBatch() { }
        private DispatchBatch(Guid societyId, Guid runnerUserId, string towerWing, string flatNumber, string status)
        {
            SocietyId = societyId;
            RunnerUserId = runnerUserId;
            TowerWing = towerWing;
            FlatNumber = UnitNumber.Create(flatNumber);
            Status = status;
            CreatedAt = DateTime.UtcNow;
        }

        public static DispatchBatch Create(Guid societyId, Guid runnerUserId, string towerWing, string flatNumber, string status, int? floor = null)
        {
            var dispatchBatch = new DispatchBatch(societyId, runnerUserId, towerWing, flatNumber, status);
            return dispatchBatch;
        }
    }
}
