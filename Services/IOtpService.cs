using SmartPackageHub_API.Models;

namespace SmartPackageHub_API.Services
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(Guid residentId);
        Task<bool> ValidateOtpAsync(Guid residentId, string code);
    }
}
