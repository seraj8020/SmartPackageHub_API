using AutoMapper;
using HubManagement.Application.Courier.Services;
using HubManagement.Application.Couriers.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Courier.Queries
{
    public sealed class ListCourierBlacklistQueryHandler
        : QueryHandler<ListCourierBlacklistQuery, CourierListResult<CourierBlacklistDto>>
    {
        private readonly IRepository<CourierBlacklist> _blacklists;
        private readonly IRepository<Society> _societies;

        public ListCourierBlacklistQueryHandler(
            IMapper mapper,
            IRepository<CourierBlacklist> blacklists,
            IRepository<Society> societies) : base(mapper)
        {
            _blacklists = blacklists;
            _societies = societies;
        }

        protected override async Task<CourierListResult<CourierBlacklistDto>> HandleAsync(ListCourierBlacklistQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var (filterSocietyId, forbidden, message) = CourierAdminAccess.TryResolveSocietyFilter(
                request.Role,
                request.SocietyIds,
                request.SocietyId);
            if (forbidden)
                return new CourierListResult<CourierBlacklistDto> { Forbidden = true, Message = message };

            var rows = (filterSocietyId.HasValue
                    ? _blacklists.FetchMulti(
                        item => item.IsActive && item.SocietyId == filterSocietyId.Value,
                        noTracking: true)
                    : _blacklists.FetchMulti(item => item.IsActive, noTracking: true))
                .OrderByDescending(item => item.BlacklistedAt)
                .ToList();

            var societyIds = rows.Select(item => item.SocietyId).Distinct().ToList();
            var societies = societyIds.Count == 0
                ? new Dictionary<Guid, string>()
                : (await _societies.ListAsync(society => societyIds.Contains(society.Id), noTracking: true)
                    .ConfigureAwait(false))
                .ToDictionary(society => society.Id, society => society.Name);

            var dtos = rows
                .Select(item => CourierDtoMapper.ToBlacklistDto(
                    item,
                    societies.GetValueOrDefault(item.SocietyId) ?? string.Empty))
                .ToList();

            return new CourierListResult<CourierBlacklistDto> { Items = dtos };
        }
    }
}