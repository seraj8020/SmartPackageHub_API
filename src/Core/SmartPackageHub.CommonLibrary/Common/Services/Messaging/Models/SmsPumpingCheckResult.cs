
namespace SmartPackageHub.Common.Services.Messaging.Models
{
    /// <summary>
    /// Allowed + optional reject Reason
    /// </summary>
    /// <param name="Allowed"></param>
    /// <param name="Reason"></param>
    public sealed record SmsPumpingCheckResult(bool Allowed, string? Reason);
}
