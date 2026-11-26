using AutoMapper;
using HubManagement.Application.Support.DTOs;
using Microsoft.Extensions.Options;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Common.ConfigOptions;

namespace HubManagement.Application.Support.Queries
{
    public sealed class GetResidentSupportConfigQueryHandler
        : QueryHandler<GetResidentSupportConfigQuery, ResidentSupportConfigDto>
    {
        private readonly ResidentSupportOptions _options;

        public GetResidentSupportConfigQueryHandler(
            IMapper mapper,
            IOptions<ResidentSupportOptions> options) : base(mapper)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options.Value;
        }

        protected override Task<ResidentSupportConfigDto> HandleAsync(GetResidentSupportConfigQuery request)
        {
            var email = string.IsNullOrWhiteSpace(_options.SupportEmail)
                ? "hello@byonverse.com"
                : _options.SupportEmail.Trim();

            return Task.FromResult(new ResidentSupportConfigDto
            {
                SupportEmail = email,
                SupportPhone = string.IsNullOrWhiteSpace(_options.SupportPhone)
                    ? null
                    : _options.SupportPhone.Trim(),
            });
        }
    }
}
