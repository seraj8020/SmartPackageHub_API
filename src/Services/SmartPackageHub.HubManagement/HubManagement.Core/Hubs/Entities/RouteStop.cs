using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class RouteStop : EntityBase
    {
        public Guid RouteId { get; private set; }
        public string TowerWing { get; private set; } = null!;
        public Guid PackageId { get; private set; }
        public int SequenceOrder { get; private set; }
        public bool IsVisited { get; private set; }

        private RouteStop() { }
        private RouteStop(Guid routeId, string buildingName, Guid packageId, int sequenceOrder)
        {
            RouteId = routeId;
            TowerWing = buildingName;
            PackageId = packageId;
            SequenceOrder = sequenceOrder;
            IsVisited = false;
        }
        public static RouteStop Create(Guid routeId, string buildingName, Guid packageId, int sequenceOrder)
        {
            if (string.IsNullOrWhiteSpace(buildingName))
                throw new ArgumentNullException(nameof(buildingName));
            if (sequenceOrder < 0)
                throw new ArgumentException("Sequence must be non-negative.", nameof(sequenceOrder));
            var routeStop = new RouteStop(routeId, buildingName, packageId, sequenceOrder);
            return routeStop;
        }

        public void MarkAsVisited() => IsVisited = true;
        public void UpdateSequence(int newSequence) => SequenceOrder = newSequence;
    }
}
