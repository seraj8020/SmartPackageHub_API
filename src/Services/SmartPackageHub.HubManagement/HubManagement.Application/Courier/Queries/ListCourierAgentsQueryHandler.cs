using AutoMapper;
using HubManagement.Application.Courier.Services;
using HubManagement.Application.Couriers.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Courier.Queries
{
    public sealed class ListCourierAgentsQueryHandler
        : QueryHandler<ListCourierAgentsQuery, CourierListResult<CourierAgentDto>>
    {
        private readonly IRepository<CourierAgent> _agents;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<CourierBlacklist> _blacklists;

        public ListCourierAgentsQueryHandler(
            IMapper mapper,
            IRepository<CourierAgent> agents,
            IRepository<Society> societies,
            IRepository<CourierBlacklist> blacklists) : base(mapper)
        {
            _agents = agents;
            _societies = societies;
            _blacklists = blacklists;
        }

        protected override async Task<CourierListResult<CourierAgentDto>> HandleAsync(ListCourierAgentsQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var (filterSocietyId, forbidden, message) = CourierAdminAccess.TryResolveSocietyFilter(
                request.Role,
                request.SocietyIds,
                request.SocietyId);
            if (forbidden)
                return new CourierListResult<CourierAgentDto> { Forbidden = true, Message = message };

            var agents = (filterSocietyId.HasValue
                    ? _agents.FetchMulti(agent => agent.SocietyId == filterSocietyId.Value, noTracking: true)
                    : _agents.FetchMulti(noTracking: true))
                .OrderBy(agent => agent.CarrierName)
                .ThenBy(agent => agent.AgentName)
                .ToList();

            var societyIds = agents.Select(agent => agent.SocietyId).Distinct().ToList();
            var societies = societyIds.Count == 0
                ? new Dictionary<Guid, string>()
                : (await _societies.ListAsync(society => societyIds.Contains(society.Id), noTracking: true)
                    .ConfigureAwait(false))
                .ToDictionary(society => society.Id, society => society.Name);

            var blacklists = (filterSocietyId.HasValue
                    ? _blacklists.FetchMulti(
                        item => item.IsActive && item.SocietyId == filterSocietyId.Value,
                        noTracking: true)
                    : _blacklists.FetchMulti(item => item.IsActive, noTracking: true))
                .ToList();

            var rows = agents.Select(agent =>
            {
                var hit = string.IsNullOrWhiteSpace(agent.Phone)
                    ? null
                    : blacklists.FirstOrDefault(item =>
                        item.SocietyId == agent.SocietyId &&
                        CourierDtoMapper.PhonesMatch(item.Phone, agent.Phone));
                return CourierDtoMapper.ToAgentDto(
                    agent,
                    societies.GetValueOrDefault(agent.SocietyId) ?? string.Empty,
                    hit);
            }).ToList();

            return new CourierListResult<CourierAgentDto> { Items = rows };
        }
    }
}