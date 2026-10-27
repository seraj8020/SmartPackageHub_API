using SmartPackageHub.Common.Services.Messaging.Models;

namespace SmartPackageHub.Common.Services.Messaging.Abstractions
{
    /// <summary>
    /// Contract to send one SMS.
    /// </summary>
    public interface ISmsSender
    {
        Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken cancellationToken = default);
    }
}
