using AutoMapper;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Intake.Queries
{
    public sealed class GetIntakeCarriersQueryHandler : QueryHandler<GetIntakeCarriersQuery, List<string>>
    {
        private readonly IRepository<CourierAgent> _agents;

        public GetIntakeCarriersQueryHandler(IMapper mapper, IRepository<CourierAgent> agents) : base(mapper)
        {
            _agents = agents;
        }

        protected override Task<List<string>> HandleAsync(GetIntakeCarriersQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            // Materialize first — Distinct/OrderBy with StringComparer cannot translate to SQL.
            var agents = (request.SocietyId is Guid societyId && societyId != Guid.Empty
                ? _agents.FetchMulti(agent => agent.IsActive && agent.SocietyId == societyId, noTracking: true)
                : _agents.FetchMulti(agent => agent.IsActive, noTracking: true)).ToList();

            var carriers = agents
                .Select(agent => agent.CarrierName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Where(name => !string.Equals(name, "Other", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Always last — not alphabetical — so the dropdown opens A–Z then Other.
            carriers.Add("Other");

            return Task.FromResult(carriers);
        }
    }
}
