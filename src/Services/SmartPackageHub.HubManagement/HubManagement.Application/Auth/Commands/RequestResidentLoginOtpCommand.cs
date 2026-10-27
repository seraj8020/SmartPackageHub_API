using HubManagement.Application.Auth.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Auth.Commands
{
    public sealed record RequestResidentLoginOtpCommand(string? PhoneNumber)
        : ICreateCommand<ResidentLoginOtpRequestResult>;
}
