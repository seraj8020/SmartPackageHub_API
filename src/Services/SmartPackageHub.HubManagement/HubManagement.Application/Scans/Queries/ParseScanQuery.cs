using HubManagement.Application.Scans.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Scans.Queries
{
    public sealed record ParseScanQuery(string? Vendor, string? Code) : Query<ScanParseResponse?>;
}
