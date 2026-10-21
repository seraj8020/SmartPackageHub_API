using HubManagement.Application.HubResidents.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed record VerifyResidentOtpCommand(Guid ResidentId, string Code) : IUpdateCommand<VerifyOtpResponse>;
    
}
