using HubManagement.Application.Auth.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Auth.Commands
{
    public sealed record VerifyResidentLoginOtpCommand(string? PhoneNumber, string? Otp)
        : ICreateCommand<ResidentLoginResult>;
}
