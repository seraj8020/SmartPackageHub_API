using HubManagement.Application.Messages.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Messages.Commands
{
    public sealed record ProcessFast2SmsWebhookCommand(Fast2SmsWebhookPayload? Payload)
        : ICreateCommand<Fast2SmsWebhookResult>;
}
