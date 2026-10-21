using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class Route : EntityBase
    {
        public Guid SocietyId { get; private set; }
        public Guid BatchId { get; private set; }
        public string OriginLocation { get; private set; }
        public string DestinationLocation { get; private set; }
        public int DistanceMetres { get; private set; }
        public string? MapProvider { get; private set; }
        public string? Polyline { get; private set; }
        public int? EstimatedDurationSecs { get; private set; }

        private readonly List<RouteStop> _stops = new List<RouteStop>();
        public IReadOnlyCollection<RouteStop> Stops => _stops.AsReadOnly();

        private Route(Guid societyId, Guid batchId, string originLocation, string destinationLocation, int distanceMetres = 0, string? mapProvider = null, string? polyline = null, int? estimatedDurationSecs = null)
        {
            SocietyId = societyId;
            BatchId = batchId;
            OriginLocation = originLocation;
            DestinationLocation = destinationLocation;
            DistanceMetres = distanceMetres;
            MapProvider = mapProvider;
            Polyline = polyline;
            EstimatedDurationSecs = estimatedDurationSecs;
        }

        public static Route Create(Guid societyId, Guid batchId, string originLocation, string destinationLocation, int distanceMetres = 0, string? mapProvider = null, string? polyline = null, int? estimatedDurationSecs = null)
        {
            if (string.IsNullOrWhiteSpace(originLocation))
                throw new ArgumentNullException(nameof(originLocation));
            if (string.IsNullOrWhiteSpace(destinationLocation))
                throw new ArgumentNullException(nameof(destinationLocation));
            var route = new Route(societyId, batchId, originLocation, destinationLocation, distanceMetres, mapProvider, polyline, estimatedDurationSecs);
            return route;
        }

        public void AddStop(string buildingName, Guid packageId)
        {
            int nextSequence = _stops.Count;
            var stop = RouteStop.Create(Id, buildingName, packageId, nextSequence);
            _stops.Add(stop);
        }

        public void OptimizeRoute(IEnumerable<Guid> orderedStopIds)
        {
            ArgumentNullException.ThrowIfNull(orderedStopIds);
            int order = 0;
            foreach (var stopId in orderedStopIds)
            {
                var stop = _stops.FirstOrDefault(s => s.Id == stopId);
                stop?.UpdateSequence(order++);
            }
            _stops.Sort((x, y) => x.SequenceOrder.CompareTo(y.SequenceOrder));
        }
    }
}
