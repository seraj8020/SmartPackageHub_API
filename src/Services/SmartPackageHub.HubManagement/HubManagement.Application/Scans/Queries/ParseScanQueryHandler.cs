using AutoMapper;
using HubManagement.Application.Scans.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Scans.Queries
{
    public sealed class ParseScanQueryHandler : QueryHandler<ParseScanQuery, ScanParseResponse?>
    {
        public ParseScanQueryHandler(IMapper mapper) : base(mapper)
        {
        }

        protected override Task<ScanParseResponse?> HandleAsync(ParseScanQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.Vendor) || string.IsNullOrWhiteSpace(request.Code))
                return Task.FromResult<ScanParseResponse?>(null);

            var tracking = ScanCodeParser.ParseTracking(request.Vendor, request.Code) ?? request.Code.Trim();
            return Task.FromResult<ScanParseResponse?>(new ScanParseResponse
            {
                Vendor = request.Vendor.Trim(),
                Raw = request.Code.Trim(),
                Tracking = tracking
            });
        }
    }
}
