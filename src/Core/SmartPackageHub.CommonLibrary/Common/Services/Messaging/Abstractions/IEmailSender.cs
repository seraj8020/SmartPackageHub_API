using SmartPackageHub.Common.Services.Messaging.Models;

namespace SmartPackageHub.Common.Services.Messaging.Abstractions
{
    public interface IEmailSender
    {
        Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default);
    }
}
