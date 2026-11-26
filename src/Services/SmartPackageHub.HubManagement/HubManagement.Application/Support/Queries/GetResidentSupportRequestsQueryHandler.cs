using AutoMapper;
using HubManagement.Application.Support.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Support.Queries
{
    public sealed class GetResidentSupportRequestsQueryHandler
        : QueryHandler<GetResidentSupportRequestsQuery, List<SupportRequestDto>?>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<SupportRequest> _requests;

        public GetResidentSupportRequestsQueryHandler(
            IMapper mapper,
            IRepository<Resident> residents,
            IRepository<SupportRequest> requests) : base(mapper)
        {
            _residents = residents;
            _requests = requests;
        }

        protected override async Task<List<SupportRequestDto>?> HandleAsync(
            GetResidentSupportRequestsQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.ResidentId == Guid.Empty)
                return null;

            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident is null)
                return null;

            var rows = await _requests.ListAsync(
                item => item.ResidentId == resident.Id,
                noTracking: true).ConfigureAwait(false);

            return rows
                .OrderByDescending(item => item.RequestedAtUtc)
                .Select(Map)
                .ToList();
        }

        internal static SupportRequestDto Map(SupportRequest row) =>
            new()
            {
                Id = row.Id,
                SocietyId = row.SocietyId,
                ResidentId = row.ResidentId,
                ReferenceCode = row.ReferenceCode,
                Subject = row.Subject,
                Category = row.Topic,
                Urgency = row.Urgency,
                Message = row.Message,
                Status = row.Status,
                SupportEmail = row.SupportEmail,
                RequestedAtUtc = row.RequestedAtUtc,
            };
    }
}
