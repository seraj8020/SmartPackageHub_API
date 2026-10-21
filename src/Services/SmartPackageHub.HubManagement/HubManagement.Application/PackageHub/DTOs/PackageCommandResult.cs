
namespace HubManagement.Application.PackageHub.DTOs
{
    public sealed record PackageCommandResult(bool Found, bool Conflict, bool Succeeded, string Message, PackageDetailsDto? Package);
}
