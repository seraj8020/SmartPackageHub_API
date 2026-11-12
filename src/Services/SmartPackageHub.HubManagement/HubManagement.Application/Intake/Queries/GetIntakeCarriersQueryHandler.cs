using AutoMapper;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Intake.Queries
{
    public sealed class GetIntakeCarriersQueryHandler : QueryHandler<GetIntakeCarriersQuery, List<string>>
    {
        private static readonly string[] FallbackCarriers =
        [
            "Amazon", "Flipkart", "Delhivery", "DTDC", "BlueDart",
            "Xpressbees", "Shadowfax", "Meesho", "Other"
        ];

        private readonly IRepository<CourierAgent> _agents;

        public GetIntakeCarriersQueryHandler(IMapper mapper, IRepository<CourierAgent> agents) : base(mapper)
        {
            _agents = agents;
        }

        protected override Task<List<string>> HandleAsync(GetIntakeCarriersQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var agents = request.SocietyId is Guid societyId && societyId != Guid.Empty
                ? _agents.FetchMulti(agent => agent.IsActive && agent.SocietyId == societyId, noTracking: true)
                : _agents.FetchMulti(agent => agent.IsActive, noTracking: true);

            var carriers = agents
                .Select(agent => agent.CarrierName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (carriers.Count == 0)
                return Task.FromResult(FallbackCarriers.ToList());

            if (!carriers.Exists(name => string.Equals(name, "Other", StringComparison.OrdinalIgnoreCase)))
                carriers.Add("Other");

            return Task.FromResult(carriers);
        }
    }
}
